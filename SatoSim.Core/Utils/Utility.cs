using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Particles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SatoSim.Core.Utils
{
	public static class Utility
	{
		public enum TextAlignment { Left, Center, Right}

		public static Vector2 CreateFromAngle(float angle)
		{
			return new Vector2(MathF.Sin(MathHelper.ToRadians(angle)), MathF.Cos(MathHelper.ToRadians(angle)));
		}

		public static void DrawNumber(this SpriteBatch spriteBatch, MonospaceFont font, int number, Vector2 position, Color color, Vector2 wholeScale, Vector2 digitScale, Vector2 digitOrigin, float spacing, float layerDepth)
		{
			DrawString(spriteBatch, font, number.ToString(), position, color, wholeScale, digitScale, digitOrigin, spacing, layerDepth, new Vector2[0]);
		}

		public static void DrawNumber(this SpriteBatch spriteBatch, MonospaceFont font, float number, Vector2 position, Color color, Vector2 wholeScale, Vector2 digitScale, Vector2 digitOrigin, float spacing, float layerDepth)
		{
            DrawString(spriteBatch, font, number.ToString(), position, color, wholeScale, digitScale, digitOrigin, spacing, layerDepth, new Vector2[0]);
        }

        public static void DrawNumber(this SpriteBatch spriteBatch, MonospaceFont font, int number, Vector2 position, Color color, Vector2 wholeScale, Vector2 digitScale, Vector2 digitOrigin, float spacing, float layerDepth, Vector2[] offsets)
		{
            DrawString(spriteBatch, font, number.ToString(), position, color, wholeScale, digitScale, digitOrigin, spacing, layerDepth, offsets);
        }

        public static void DrawNumber(this SpriteBatch spriteBatch, MonospaceFont font, float number, Vector2 position, Color color, Vector2 wholeScale, Vector2 digitScale, Vector2 digitOrigin, float spacing, float layerDepth, Vector2[] offsets)
		{
            DrawString(spriteBatch, font, number.ToString(), position, color, wholeScale, digitScale, digitOrigin, spacing, layerDepth, offsets);
        }

		public static void DrawString(this SpriteBatch spriteBatch, MonospaceFont font, string text, Vector2 position, Color color, Vector2 wholeScale, Vector2 characterScale, Vector2 characterOrigin, float spacing, float layerDepth, Vector2[]? offsets, TextAlignment alignment = TextAlignment.Center)
		{
			char[] txt = text.ToString().ToCharArray();
			Vector2[] pos = new Vector2[txt.Length];

			for (int i = 0; i < pos.Length; i++)
			{
				pos[i] = position + new Vector2((font.GlyphWidth * characterScale.X + spacing) * i, 0) * wholeScale;

				if (offsets != null && offsets.Length > i)
					pos[i] += offsets[i];
			}

			Vector2 alignmentOffset = Vector2.Zero;
			if(alignment == TextAlignment.Center)
			{
				alignmentOffset.X -= ((font.GlyphWidth * characterScale.X + spacing) * (txt.Length / 2f) - spacing / 2f - font.GlyphWidth * characterScale.X / 2f) * wholeScale.X;
            }
			else
			if(alignment == TextAlignment.Right)
			{
                alignmentOffset.X -= ((font.GlyphWidth * characterScale.X + spacing) * (txt.Length) - spacing * 2) * wholeScale.X;
            }

            for (int i = 0; i < text.Length; i++)
			{
				spriteBatch.Draw(font.GetGlyphTexture(text[i]), pos[i] + (alignmentOffset), color, 0f, characterOrigin, characterScale * wholeScale, SpriteEffects.None, layerDepth);
                //spriteBatch.DrawCircle(pos[i] + (alignmentOffset), 3f, 16, Color.Green, 2f, 1f);
            }

            //spriteBatch.DrawCircle(position, 5f, 16, Color.Magenta, 5f, 0f);
		}

		public static string CalculateMD5(string filename)
		{
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(filename))
				{
					var hash = md5.ComputeHash(stream);
					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
		}
		public class MonospaceFont
		{
			private Dictionary<char, int> _glyphIDs;
			private Texture2DAtlas _atlas;
			public int GlyphWidth { get; private set; }
			public int GlyphHeight { get; private set; }

			public MonospaceFont(Texture2D atlasTexture, int glyphWidth, int glyphHeight, string glyphs, int glyphMargin = 0, int glyphSpacing = 0)
			{
				_atlas = Texture2DAtlas.Create("monofont", atlasTexture, glyphWidth, glyphHeight, margin: glyphMargin, spacing: glyphSpacing);

				_glyphIDs = new Dictionary<char, int>();

				GlyphWidth = glyphWidth;
				GlyphHeight = glyphHeight;

				for(int c = 0; c < glyphs.Length; c++)
				{
					if (!_glyphIDs.ContainsKey(glyphs[c]))
					{
						_glyphIDs.Add(glyphs[c], c);
					}
				}
			}

			public Texture2DRegion GetGlyphTexture(char glyph)
			{
				if (_glyphIDs.ContainsKey(glyph))
					return _atlas[_glyphIDs[glyph]];
				else
					return _atlas[_glyphIDs[' ']];
			}
		}

		public struct HierarchyTree<T>
		{
			public List<HierarchyBranch<T>> Root { get; set; }
			public HierarchyBranch<T> ActiveBranch;

            public Stack<int> HierarchyStack { get; set; }

			public HierarchyTree()
			{
				Root = new List<HierarchyBranch<T>>();
                ActiveBranch = new HierarchyBranch<T>()
                {
                    SubBranches = Root
                };
				HierarchyStack = new Stack<int>();
            }

            // TODO: Add code to find active branch
            public void GoDown(int id)
			{
				HierarchyStack.Push(id);

				if (HierarchyStack.Count == 0)
					ActiveBranch = Root[id];
				else
					ActiveBranch = ActiveBranch.SubBranches[id];
			}

			public void GoUp()
			{
				if (HierarchyStack.Count > 0)
					HierarchyStack.Pop();

                if (HierarchyStack.Count == 0)
					ActiveBranch = new HierarchyBranch<T>()
					{
						SubBranches = Root
					};
				else
				{
                    foreach (int i in HierarchyStack)
                        ActiveBranch = ActiveBranch.SubBranches[i];
                }
            }

			public void SetActiveToRoot()
			{
                ActiveBranch = new HierarchyBranch<T>()
                {
                    SubBranches = Root
                };
            }
        }

		public struct HierarchyBranch<T>
		{
			public T BranchObject;
			public List<HierarchyBranch<T>> SubBranches;

            public HierarchyBranch(T branchObject)
            {
                BranchObject = branchObject;
                SubBranches = new List<HierarchyBranch<T>>();
            }
			public HierarchyBranch(T branchObject, T[] subBranches)
			{
				BranchObject = branchObject;
				SubBranches = new List<HierarchyBranch<T>>();

				foreach(T b in subBranches)
				{
					SubBranches.Add(new HierarchyBranch<T>(b));
				}
			}

			public void AddBranch(HierarchyBranch<T> branch)
			{
				SubBranches.Add(branch);
			}

			public void RemoveBranch(int index)
			{
				SubBranches.RemoveAt(index);
			}

			public void RemoveBranch(HierarchyBranch<T> branch)
			{
				SubBranches.Remove(branch);
			}

			public void SetBranches(List<HierarchyBranch<T>> branches)
			{
				SubBranches.Clear();
				foreach (var b in branches) SubBranches.Add(b);
			}
        }
    }
}
