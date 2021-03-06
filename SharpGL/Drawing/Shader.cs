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
    /// <summary>
    /// A draw hint contains information about how to handle given Vertex data in the Shader
    /// </summary>
	public struct AttributeHint
	{
		public string attributeName;
		public int components;
		public int stride;
		public int offset;
		public bool normalize;

		public AttributeHint(string attributeName, int components, int stride, int offset, bool normalize)
		{
			this.attributeName = attributeName;
			this.components = components;
			this.stride = stride;
			this.offset = offset;
			this.normalize = normalize;
		}
	}
   
    /// <summary>
    /// A Shader creates and binds OpenGL shaders and provides Methods to apply shader parameters
    /// </summary>
    public class Shader : IDisposable
    {
        const string Identifier = "[Shader %s]";
        const string IdentRegex = "\\[Shader\\s+([a-zA-Z\\d]*)\\]";
        private bool disposed = false;
        private string vertexIdent, geometryIdent, fragmentIdent;
        /// <summary>
        /// Vertex shader location
        /// </summary>
		public int LocVertex { get; private set; }
        /// <summary>
        /// Geometry shader location
        /// </summary>
		public int LocGeometry { get; private set; }
        /// <summary>
        /// Fragment shader location
        /// </summary>
		public int LocFragment { get; private set; }
		public int Program { get; private set; }
        private Dictionary<string, int> uniformLocations;
		private Dictionary<string, int> vertexAttributeLocations;
		private string filePath;
        /// <summary>
        /// Creates a shader
        /// </summary>
        /// <param name="filePath">The text-file containing the shader data</param>
        /// <param name="vertexIdent">The identifier of the vertex shader</param>
        /// <param name="geometryIdent">The identifier of the geometry shader</param>
        /// <param name="fragmentIdent">The identifier of the fragment shader</param>
		public Shader(string filePath, string vertexIdent, string geometryIdent, string fragmentIdent)
        {
			this.filePath = filePath;
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
				string err = GL.GetProgramInfoLog(Program);
				if (err.Length > 0)
					Log.Error(String.Format("Shader Error in {0}: v/g/a | {1}/{2}/{3} | Error: {4}", filePath, vertexIdent, geometryIdent, fragmentIdent, err));
				else
					Log.Error(String.Format("Shader uploaded successfully {0}: v/g/a | {1}/{2}/{3}", filePath, vertexIdent, geometryIdent, fragmentIdent));
            }
            else
            {
                Log.Error("Shader file does not exist: " + filePath);
            }
        }
        /// <summary>
        /// Uses the Shader
        /// </summary>
        public void Use()
        {
            GL.UseProgram(Program);
        }
        /// <summary>
        /// Applies vertex attributes
        /// </summary>
        /// <param name="drawHints">One or multiple vertex draw hints containing information about how to handle vertex data</param>
		public void SetVertexAttributes(params AttributeHint[] drawHints)
		{
			foreach (var h in drawHints)
			{
				if (h.attributeName != null)
				{
					int posAtt = GL.GetAttribLocation(Program, h.attributeName);
					if (posAtt >= 0)
					{
						GL.EnableVertexAttribArray(posAtt);
						
						GL.VertexAttribPointer(posAtt, h.components, VertexAttribPointerType.Float, h.normalize, h.stride * sizeof(float), h.offset * sizeof(float));
						//GL.DisableVertexAttribArray(posAtt);
					}
				}
			}
		}
        /// <summary>
        /// Sets a uint uniform value at the given location
        /// </summary>
        /// <param name="location">The location of the uniform value in the current shader program</param>
        /// <param name="value">The value to pass to the shader program</param>
		public void SetUniform(int location, uint value) 
		{
			GL.Uniform1(location, value);
		}
        /// <summary>
        /// Sets a float uniform value at the given location
        /// </summary>
        /// <param name="location">The location of the uniform value in the current shader program</param>
        /// <param name="value">The value to pass to the shader program</param>
		public void SetUniform(int location, float value)
		{
			GL.Uniform1(location, value);
		}
        /// <summary>
        /// Sets a double uniform value at the given location
        /// </summary>
        /// <param name="location">The location of the uniform value in the current shader program</param>
        /// <param name="value">The value to pass to the shader program</param>
		public void SetUniform(int location, double value)
		{
			GL.Uniform1(location, value);
		}
        /// <summary>
        /// Sets the value of the given uniform variable.
        /// </summary>
        /// <typeparam name="T">The data type of the uniform variable</typeparam>
        /// <param name="name">The name of the uniform variable in the shader program</param>
        /// <param name="values">A list of values which will be passed to the uniform location</param>
		public void SetUniform<T>(string name, params T[] values)
        {
            SetUniform(name, typeof(T), values);
			
        }
        /// <summary>
        /// Sets the value of the given uniform variable.
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader program</param>
        /// <param name="type">The data type of the uniform variable</typeparam>
        /// <param name="values">A list of values which will be passed to the uniform location</param>
        public void SetUniform(string name, Type type, object values)
        {
            if (type == typeof(float))
            {
                float[] data = (float[])values;
				switch (data.Length)
				{
					case 0:
						break;
					case 1:
						GL.Uniform1(GetUniformLoc(name), data[0]);
						break;
					case 2:
						GL.Uniform2(GetUniformLoc(name), 1, data);
						break;
					case 3:
						GL.Uniform3(GetUniformLoc(name), 1, data);
						break;
					case 4:
						GL.Uniform4(GetUniformLoc(name), 1, data);
						break;
					default:
						throw (new Exception("Only a maximum of four values can be passed to a uniform"));
				}
            }
            else if (type == typeof(int))
            {
                int[] data = (int[])values;
				switch (data.Length)
				{
					case 0:
						break;
					case 1:
						GL.Uniform1(GetUniformLoc(name), data[0]);
						break;
					case 2:
						GL.Uniform2(GetUniformLoc(name), 2, data);
						break;
					case 3:
						GL.Uniform2(GetUniformLoc(name), 3, data);
						break;
					case 4:
						GL.Uniform2(GetUniformLoc(name), 4, data);
						break;
					default:
						throw (new Exception("Only a maximum of four values can be passed to a uniform"));
				}
            }
			else if(type == typeof(Matrix4))
			{
				Matrix4[] data = (Matrix4[])values;
				GL.UniformMatrix4(GetUniformLoc(name), false, ref data[0]);
			}
            else
                throw (new NotImplementedException("type " + type + " not supported"));
        }
		
        /// <summary>
        /// Returns the location of a uniform variable in the shader program
        /// </summary>
        /// <param name="name">The name of the uniform variable</param>
        /// <returns>The location of the uniform variable</returns>
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
        /// <summary>
        /// Applies the shader program to a Surface
        /// </summary>
        /// <param name="surface">The surface to be used as source and target</param>
        /// <param name="parameters">Additional shader parameters</param>
        public void ApplyTo(Surface surface, params ShaderParamBase[] parameters)
        {
			using (Surface pong = new Surface(surface.Width, surface.Height))
            {
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
        /// <summary>
        /// Renders the source to the target using this shader
        /// </summary>
        /// <param name="source">Source Surface</param>
        /// <param name="target">Target Surface</param>
        /// <param name="parameters">Additional shader parameters</param>
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
			if (index < actualIdent.Length)
				throw (new Exception("Ident " + ident + " not found in " + filePath));
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
        private void ApplyMultiUniform<T>(int location, int length, T[] data)
        {
            throw (new NotImplementedException("This is not implemented"));
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
