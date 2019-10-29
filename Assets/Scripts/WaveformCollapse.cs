using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveformCollapse : MonoBehaviour
{
	const int INPUT_WIDTH = 5;
	const int INPUT_HEIGHT = 5;

	[SerializeField]
	public MeshRenderer InputRenderer;
	[SerializeField]
	public MeshRenderer OutputRenderer;
	[SerializeField]
	public int OutputSizeX = 32;
	[SerializeField]
	public int OutputSizeY = 32;

	public struct Tile
	{
		public Color[] Colors;
	}

	public class TileStack : List<Tile> { }

	List<List<Color>> _input;
	
	Texture2D _inputTexture;
	Texture2D _outputTexture;

	IEnumerator Start()
	{
		// Step 1: Input Texture 5x5, white outer, black ring, red inner
		SetInput();

		// Step 2: Render The Input Texture
		CreateInputTexture();
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
		int lowestEntropyStackIndex = Random.Range(0, pixelCount); // Pick random first stack


		// Step 6: Collapse the Waveform!
		bool allStacksResolved = false;
		while (!allStacksResolved)
		{
			// Pick Random Tile from lowest entropy stack
			Tile resolvedTile = PickRandomTile(tileStacks, lowestEntropyStackIndex);
			// TODO: Only remove if there are valid options remaining on all adjacent pixels?
			outputColors[lowestEntropyStackIndex] = resolvedTile.Colors[4];

			int x = lowestEntropyStackIndex % OutputSizeX, y = lowestEntropyStackIndex / OutputSizeX;
			int stackIndex = 0;
			if (x - 1 > 0)
			{
				if (y - 1 > 0)
				{
					stackIndex = (x - 1) + (y - 1) * OutputSizeX;
					// Remove invalid tiles where overlap does not match
					// TODO: Check there are any function instead
				}
			}


			allStacksResolved = CalculateAreAllStacksResolved(tileStacks);
			yield return null;
		}
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
				if (i == 0 || i == 4 || j == 0 || j == 4)
				{
					row.Add(Color.white);
				}
				else if (i == 2 && j == 2)
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

	private void CreateInputTexture()
	{
		_inputTexture = new Texture2D(INPUT_WIDTH, INPUT_HEIGHT);
		for (int x = 0; x < INPUT_WIDTH; x++)
		{
			for (int y = 0; y < INPUT_HEIGHT; y++)
			{
				_inputTexture.SetPixel(x, y, _input[y][x]);
			}
		}
		_inputTexture.Apply();
	}
}
