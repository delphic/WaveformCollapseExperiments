using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveformCollapse : MonoBehaviour
{
	const int INPUT_WIDTH = 11;
	const int INPUT_HEIGHT = 11;

	[SerializeField]
	public MeshRenderer InputRenderer;
	[SerializeField]
	public MeshRenderer OutputRenderer;
	[SerializeField]
	public int OutputSizeX = 32;
	[SerializeField]
	public int OutputSizeY = 32;

	HashSet<int> _consideredStacks = new HashSet<int>();

	public struct Tile
	{
		public Color[] Colors;
		public Color GetColor(int x, int y)
		{
			return Colors[x + y * 3];
		}
		public bool HasValidOverlap(Color color, int xOffset, int yOffset)
		{
			int x = 1 - xOffset, y = 1 - yOffset;
			return GetColor(x, y) == color;
		}
	}

	public class TileStack : List<Tile>
	{
		public int Entropy { get { return this.Count; } }
	}

	List<List<Color>> _input;
	
	Texture2D _inputTexture;
	Texture2D _outputTexture;

	IEnumerator Start()
	{
		// Create Input Textures
		_inputTexture = new Texture2D(INPUT_WIDTH, INPUT_HEIGHT);
		_outputTexture = new Texture2D(OutputSizeX, OutputSizeY);

		// Step 1: Input Texture 5x5, white outer, black ring, red inner
		SetInput();

		// Step 2: Render The Input Texture
		SetInputTextureColors();
		InputRenderer.material.mainTexture = _inputTexture;

		// Step 3: Profit, create 3x3 tiles
		List<Tile> tiles = CreateTiles();

		// Step 4: Create Stacks
		int pixelCount = OutputSizeX * OutputSizeY;
		TileStack[] tileStacks = new TileStack[pixelCount];
		for (int i = 0; i < pixelCount; i++)
		{
			tileStacks[i] = new TileStack();
			tileStacks[i].AddRange(tiles);
		}

		yield return null;

		// Step 5: Pick random pixel and tile, set it's color
		Color[] outputColors = new Color[pixelCount];
		for(int i = 0; i < pixelCount; i++)
		{
			outputColors[i] = Color.magenta;
		}

		int lowestEntropyStackIndex = Random.Range(0, pixelCount); // Pick random first stack

		// Step 6: Collapse the Waveform!
		bool allStacksResolved = false;
		while (!allStacksResolved)
		{
			// Pick Random Tile from lowest entropy stack
			Tile resolvedTile = PickRandomTile(tileStacks, lowestEntropyStackIndex);
			_consideredStacks.Add(lowestEntropyStackIndex);

			// TODO: Only pick tilesif there are valid options remaining on all adjacent pixels? 
			// Q: This should happen automatically with a valid input set?
			// Might also be possible to backtrack if all stacks are removed by a choice, however an input validator is probably easier

			outputColors[lowestEntropyStackIndex] = resolvedTile.GetColor(1,1);
			// TODO: Stamp full tile not just center position

			int x = lowestEntropyStackIndex % OutputSizeX, y = lowestEntropyStackIndex / OutputSizeX;
			int stackIndex = 0;
			for(int xOffset = x - 1; xOffset <= x + 1; xOffset++)
			{
				for(int yOffset = y -1; yOffset <= y + 1; yOffset++)
				{
					if (xOffset >= 0 && yOffset >= 0 && xOffset < OutputSizeX && yOffset < OutputSizeY && !(xOffset == x && yOffset == y))
					{
						stackIndex = xOffset + yOffset * OutputSizeX;
						var stack = tileStacks[stackIndex];
						for (int index = stack.Count - 1; index >= 0; index--)
						{
							// TODO: Check against all resolved output colours
							if (!stack[index].HasValidOverlap(outputColors[lowestEntropyStackIndex], xOffset - x, yOffset - y))
							{
								if (stack.Count > 1)
								{
									stack.RemoveAt(index);
								}
								else
								{
									Debug.LogError("Ran out of valid tiles");
								}
							}
						}
					}
				}
			}

			allStacksResolved = _consideredStacks.Count == tileStacks.Length;

			if (!allStacksResolved)
			{
				lowestEntropyStackIndex = GetLowestEntropyStackIndex(tileStacks);
			}

			SetOutputTextureColors(outputColors);
			OutputRenderer.material.mainTexture = _outputTexture;

			yield return null;
		}

		// Step 7: Render the output
		SetOutputTextureColors(outputColors);
		OutputRenderer.material.mainTexture = _outputTexture;
	}

	private int GetLowestEntropyStackIndex(TileStack[] tileStacks)
	{
		int lowestEntropyStackIndex;
		// Get lowest entropy stack
		List<int> potentialStackIndices = new List<int>();
		int minimumEntropy = int.MaxValue;
		for (int i = 0; i < tileStacks.Length; i++)
		{
			if (!_consideredStacks.Contains(i) && tileStacks[i].Entropy <= minimumEntropy)
			{
				if (tileStacks[i].Entropy < minimumEntropy)
				{
					potentialStackIndices.Clear();
					minimumEntropy = tileStacks[i].Entropy;
				}
				potentialStackIndices.Add(i);
			}
		}

		lowestEntropyStackIndex = potentialStackIndices[Random.Range(0, potentialStackIndices.Count)];
		return lowestEntropyStackIndex;
	}

	private static Tile PickRandomTile(TileStack[] tileStacks, int stackIndex)
	{
		Tile resolvedTile;
		int randomTileIndex = Random.Range(0, tileStacks[stackIndex].Count);
		for (int i = tileStacks[stackIndex].Count - 1; i >= 0; i--)
		{
			if (i != randomTileIndex)
			{
				tileStacks[stackIndex].RemoveAt(i);
			}
		}
		resolvedTile = tileStacks[stackIndex][0];
		return resolvedTile;
	}

	private static bool CalculateAreAllStacksResolved(TileStack[] tileStacks)
	{
		bool allStacksResolved = true;
		for (int i = 0, l = tileStacks.Length; i < l; i++)
		{
			if (tileStacks[i].Count > 1)
			{
				allStacksResolved = false;
				break;
			}
		}

		return allStacksResolved;
	}

	private List<Tile> CreateTiles()
	{
		int xCount = INPUT_WIDTH - 2;
		int yCount = INPUT_HEIGHT - 2;
		var tiles = new List<Tile>();
		for (int i = 0; i < xCount; i++)
		{
			for (int j = 0; j < yCount; j++)
			{
				var tile = new Tile { Colors = new Color[9] };
				int index = 0;
				for (int x = 0; x < 3; x++)
				{
					for (int y = 0; y < 3; y++)
					{
						tile.Colors[index] = _input[j + y][i + x];
						index += 1;
					}
				}
				tiles.Add(tile);
			}
		}
		// TODO: Remove dupes and have weightings
		return tiles;
	}

	private void Debug_RenderTiles(int xCount, int yCount, List<Tile> tiles)
	{
		_outputTexture = new Texture2D(xCount * 4 - 1, yCount * 4 - 1);
		for (int index = 0, count = tiles.Count; index < count; index++)
		{
			int xOffset = 4 * (index % xCount);
			int yOffset = 4 * (index / xCount);
			for (int x = 0; x < 3; x++)
			{
				for (int y = 0; y < 3; y++)
				{
					_outputTexture.SetPixel(xOffset + x, yOffset + y, tiles[index].Colors[x + 3 * y]);
				}
			}
		}
		_outputTexture.Apply();
		OutputRenderer.material.mainTexture = _outputTexture;
	}

	private void SetInput()
	{
		_input = new List<List<Color>>();
		for (int i = 0; i < INPUT_HEIGHT; i++)
		{
			var row = new List<Color>();
			for (int j = 0; j < INPUT_WIDTH; j++)
			{
				if (i == 0 || i == 1 || i == 2 || i == INPUT_WIDTH - 3 || i == INPUT_WIDTH - 2 || i == INPUT_WIDTH - 1
					|| j == 0 || j == 1 || j == 2 || j == INPUT_WIDTH -3 || j == INPUT_HEIGHT - 2 || j == INPUT_HEIGHT - 1)
				{
					row.Add(Color.white);
				}
				else if (i == INPUT_WIDTH / 2 && j == INPUT_HEIGHT / 2)
				{
					row.Add(Color.red);
				}
				else
				{
					row.Add(Color.black);
				}
			}
			_input.Add(row);
		}
	}

	private void SetInputTextureColors()
	{
		for (int x = 0; x < INPUT_WIDTH; x++)
		{
			for (int y = 0; y < INPUT_HEIGHT; y++)
			{
				_inputTexture.SetPixel(x, y, _input[y][x]);
			}
		}
		_inputTexture.Apply();
	}

	private void SetOutputTextureColors(Color[] colors)
	{
		for (int i = 0, l = colors.Length; i < l; i++)
		{
			_outputTexture.SetPixel(i % OutputSizeX, i / OutputSizeX, colors[i]);
		}
		_outputTexture.Apply();
	}
}
