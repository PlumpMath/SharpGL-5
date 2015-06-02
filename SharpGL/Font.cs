﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using SharpGL.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace SharpGL
{
    /// <summary>
    /// A Font holds a spritesheet for a font which can be rendered
    /// </summary>
	public class Font
	{
		private const int maxWidth = 2048;
		private Dictionary<char, Vector2> charSizes;
		private Dictionary<char, Vector2> charLocations;
		private Texture2D texture;
        /// <summary>
        /// Creates a Font
        /// </summary>
        /// <param name="font">The name of the Font (E.g. "Arial")</param>
        /// <param name="characters">A string containing the characters the sprite sheet should contain. Only these characters can be rendered.</param>
        /// <param name="size">Size in points of the font</param>
		public Font(string font, string characters, float size)
		{
			//measure space
			charSizes = new Dictionary<char, Vector2>();
			charLocations = new Dictionary<char, Vector2>();
			System.Drawing.Font f = new System.Drawing.Font(font, size);
			using (var gTemp = Graphics.FromHwnd(IntPtr.Zero))
			{
				
				char[] chars = characters.ToCharArray();
				foreach (var c in chars)
				{
					if (!charSizes.ContainsKey(c)) 
					{
						var s = gTemp.MeasureString(c + "", f);
						charSizes.Add(c, new Vector2(s.Width, s.Height));
					}
				}
			}
			float width = 0;
			float lineWidth = 0;
			float height = 0;
			float lineHeight = 0;
			foreach(var entry in charSizes)
			{
				if (lineWidth + entry.Value.X < maxWidth)
					lineWidth += entry.Value.X;
				else
				{
					width = maxWidth;
					height += lineHeight;
					lineHeight = 0;
					lineWidth = 0;
				}
				if (entry.Value.Y > lineHeight)
					lineHeight = entry.Value.Y;
			}
			height += lineHeight;
			width = Math.Max(lineWidth, width);
			
			Bitmap fontTarget = new Bitmap((int)(0.5f + width), (int)(0.5f + height));
			using (Graphics gr = Graphics.FromImage(fontTarget))
			{
				gr.TextRenderingHint = TextRenderingHint.AntiAlias;	
				float x = 0;
				float y = 0;
				float columnHeight = -1;
				gr.Clear(Color.Transparent);
				foreach (var entry in charSizes)
				{
					if (x + entry.Value.X >= maxWidth)
					{
						x = 0;
						y += columnHeight;
						columnHeight = 0;
					}
					gr.DrawString(entry.Key + "", f, new SolidBrush(Color.White), new PointF(x, y));
					charLocations.Add(entry.Key, new Vector2(x, y));
					x += entry.Value.X;
					if (entry.Value.Y > columnHeight)
						columnHeight = entry.Value.Y;
				}
			}
		//	fontTarget.Save("test.png",	ImageFormat.Png);
			BitmapData data = fontTarget.LockBits(new Rectangle(0, 0, fontTarget.Width, fontTarget.Height), ImageLockMode.ReadOnly, fontTarget.PixelFormat);
            SurfaceFormat format = SurfaceFormat.Texture2DAlpha;
            format.Pixels = data.Scan0;
            format.DepthBuffer = false;
            format.Multisampling = 0;
            format.PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
            format.SourceType = PixelType.UnsignedByte;
			texture = new Texture2D(fontTarget.Width, fontTarget.Height,format);
			fontTarget.UnlockBits(data);
		}
        /// <summary>
        /// Draws a string to a surface.
        /// </summary>
        /// <param name="surface">The surface to draw to.</param>
        /// <param name="shader">The shader to use for drawing.</param>
        /// <param name="text">The string to draw.</param>
        /// <param name="charDist">The additional distance between characters. Can be negative.</param>
        /// <param name="basePos">The location of the string. (Top-left)</param>
        /// <param name="color">The color to use for drawing.</param>
		public void DrawString(Surface surface, Shader shader, string text, float charDist, Vector2 basePos, Vector4 color)
		{
            if (surface != null)
            {
                float xAt = -1;
                float yAt = -1;
                Vector2 texCoord;
                Vector2 size;
                Vector2 sizeQ;
                char[] chars = text.ToCharArray();
                GL.Enable(EnableCap.Texture2D);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.BlendEquation(BlendEquationMode.Max);
                GL.Disable(EnableCap.DepthTest);
                shader.Use();
                shader.SetUniform<float>("_color", color.X, color.Y, color.Z, color.W);
                surface.BindFramebuffer();
                texture.BindTexture();
                basePos = new Vector2(basePos.X / (float)surface.Width, basePos.Y / (float)surface.Height);
                GL.Begin(PrimitiveType.Quads);

                foreach (var c in chars)
                {
                    if (charLocations.ContainsKey(c))
                    {
                        Vector2 scale = new Vector2(surface.Width / (float)texture.Width, surface.Height / (float)texture.Height);
                        texCoord = new Vector2(charLocations[c].X / texture.Width, charLocations[c].Y / texture.Height);
                        size = new Vector2(charSizes[c].X / texture.Width, charSizes[c].Y / texture.Height);
                        sizeQ = new Vector2(size.X / scale.X, size.Y / scale.Y);
                        GL.TexCoord2(texCoord.X, texCoord.Y + size.Y);
                        GL.Vertex2(basePos.X + xAt, basePos.Y + yAt);

                        GL.TexCoord2(texCoord.X + size.X, texCoord.Y + size.Y);
                        GL.Vertex2(basePos.X + xAt + sizeQ.X, basePos.Y + yAt);

                        GL.TexCoord2(texCoord.X + size.X, texCoord.Y);
                        GL.Vertex2(basePos.X + xAt + sizeQ.X, basePos.Y + yAt + sizeQ.Y);

                        GL.TexCoord2(texCoord.X, texCoord.Y);
                        GL.Vertex2(basePos.X + xAt, basePos.Y + yAt + sizeQ.Y);


                        xAt += sizeQ.X * charDist;
                        //y += sizeQ.Y;
                    }
                }
                GL.End();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.UseProgram(0);
            }
		}
	}
}
