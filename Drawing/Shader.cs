﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpGL.Components;
namespace SharpGL.Drawing
{
	public struct VertexObjectDrawHint
	{
		public string attributeName;
		public int components;
		public int stride;
		public int offset;

		public VertexObjectDrawHint(string attributeName, int components, int stride, int offset)
		{
			this.attributeName = attributeName;
			this.components = components;
			this.stride = stride;
			this.offset = offset;
		}
	}
    public class Shader : IDisposable
    {
        const string Identifier = "[Shader %s]";
        const string IdentRegex = "\\[Shader\\s+([a-zA-Z\\d]*)\\]";
        private bool disposed = false;
        private string vertexIdent, geometryIdent, fragmentIdent;
		public int LocVertex { get; private set; }
		public int LocGeometry { get; private set; }
		public int LocFragment { get; private set; }
		public int Program { get; private set; }
        private Dictionary<string, int> uniformLocations;
		private Dictionary<string, int> vertexAttributeLocations;
		public Shader(string filePath, string vertexIdent, string geometryIdent, string fragmentIdent)
        {
            this.vertexIdent = vertexIdent;
			this.geometryIdent = geometryIdent;
            this.fragmentIdent = fragmentIdent;
			uniformLocations = new Dictionary<string, int>();
			vertexAttributeLocations = new Dictionary<string, int>();
            if(File.Exists(filePath))
            {
                string data = File.ReadAllText(filePath);
				Program = GL.CreateProgram();
				if (vertexIdent != null)
				{
					string vertex = GetShaderString(data, vertexIdent);
					LoadShader(Program, vertex, ShaderType.VertexShader);
				}
				if (geometryIdent != null)
				{
					string geometry = GetShaderString(data, geometryIdent);
					LoadShader(Program, geometry, ShaderType.GeometryShader);
				}
				if (fragmentIdent != null)
				{
					string fragment = GetShaderString(data, fragmentIdent);
					LoadShader(Program, fragment, ShaderType.FragmentShader);
				}
                GL.LinkProgram(Program);
                Log.Error(vertexIdent + "/" + geometryIdent + "/" + fragmentIdent + ": " + GL.GetProgramInfoLog(Program));
            }
            else
            {
                Log.Error("Shader file does not exist: " + filePath);
            }
        }
        public void Use()
        {
            GL.UseProgram(Program);
            
        }
		public void SetVertexAttributes(params VertexObjectDrawHint[] drawHints)
		{
			foreach (var h in drawHints)
			{
				if (h.attributeName != null)
				{
					int posAtt = GL.GetAttribLocation(Program, h.attributeName);
					if (posAtt >= 0)
					{
						GL.EnableVertexAttribArray(posAtt);
						
						GL.VertexAttribPointer(posAtt, h.components, VertexAttribPointerType.Float, false, h.stride * sizeof(float), h.offset * sizeof(float));
						//GL.DisableVertexAttribArray(posAtt);
					}
				}
			}
		}
        public void SetUniform<T>(string name, object values)
        {
            SetUniform(name, typeof(T), values);
        }
        public void SetUniform(string name, Type type, object values)
        {
            if (type == typeof(float))
            {
                float[] data = (float[])values;
                GL.Uniform1(GetUniformLoc(name), data.Length, data);
            }
            else if (type == typeof(int))
            {
                int[] data = (int[])values;
                GL.Uniform1(GetUniformLoc(name), data.Length, data);
            }
			else if(type == typeof(Matrix4))
			{
				Matrix4 data = (Matrix4)values;
				GL.UniformMatrix4(GetUniformLoc(name), false, ref data);
			}
            else
                throw (new NotImplementedException("type " + type + " not supported"));
        }
        public int GetUniformLoc(string name)
        {
            int loc=-1;
            if (!uniformLocations.TryGetValue(name, out loc))
            {
                loc = GL.GetUniformLocation(Program, name);
                uniformLocations.Add(name, loc);
				Log.Write(name + " Location: " + loc);
            }
            return loc;
        }
        public void ApplyTo(Surface surface, params ShaderParamBase[] parameters)
        {
            using (Surface pong = new Surface())
            {
                pong.Create(surface.Width, surface.Height);
                pong.Clear();

                GL.Viewport(0, 0, surface.Width, surface.Height);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, 1.0, 1.0, 0.0, 0.0, 4.0);
                GL.UseProgram(0);
                surface.BindTexture();
                pong.BindFramebuffer();


                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex3(0, 0, 0);
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex3(0, 1, 0);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex3(1, 1, 0);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex3(1, 0, 0);
                GL.End();
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                surface.Clear();
                Use(); // calls GL.UseProgram()
                foreach (var p in parameters)
                {
                    p.Apply(this);
                }
                pong.BindTexture();
                surface.BindFramebuffer();


                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex3(0, 0, 0);
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex3(0, 1, 0);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex3(1, 1, 0);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex3(1, 0, 0);
                GL.End();
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }
        public void ApplyTo(Surface source, Surface target, params ShaderParamBase[] parameters)
        {
            GL.Viewport(0, 0, target.Width, target.Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, 1.0, 1.0, 0.0, 0.0, 4.0);
            GL.UseProgram(0);
            target.Clear();
            Use(); // calls GL.UseProgram()
            foreach(var p in parameters)
            {
                p.Apply(this);
            }
            source.BindTexture();
            target.BindFramebuffer();


            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex3(0, 0, 0);
            GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex3(0, 1, 0);
            GL.TexCoord2(1.0f, 0.0f);
            GL.Vertex3(1, 1, 0);
            GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex3(1, 0, 0);
            GL.End();
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
        }
        private string GetShaderString(string data, string ident)
        {
            string actualIdent = Identifier.Replace("%s", ident);
            int index = data.IndexOf(actualIdent) + actualIdent.Length;
            int len = data.Length - index;
            var nextIdent = Regex.Match(data.Substring(index), IdentRegex);
            if(nextIdent.Success)
            {
                len = nextIdent.Index;
            }
            return data.Substring(index, len);
        }
        private void LoadShader(int program, string data, ShaderType t)
        {
            int s = GL.CreateShader(t);
			switch(t)
			{
				case ShaderType.VertexShader:
					LocVertex = s;
					break;
				case ShaderType.GeometryShader:
					LocGeometry = s;
					break;
				case ShaderType.FragmentShader:
					LocFragment = s;
					break;
			}
            GL.ShaderSource(s, data);
            GL.CompileShader(s);
			string err = GL.GetShaderInfoLog(s);
            Log.Error(t.ToString() +": " + err);
            GL.AttachShader(program, s);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Log.Write("Disposing shader");
                GL.DeleteProgram(Program);
            }

            disposed = true;
        }
    }
}
