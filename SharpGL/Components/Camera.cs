﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using SharpGL.Drawing;
using SharpGL.Components;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace SharpGL.Components
{
	public class Camera : Component
	{
		private bool beganDraw;
		private float zNear;
		private float zFar;
		private float fov;
		private Surface multisampler;
		protected Matrix4 projectionMatrix;
		public Material CameraMaterial { get; private set; }
		private Mesh screenMesh;
		public int VAO { get; private set; }
		public bool Multisampling
		{
			get
			{
				return multisampler != null;
			}
		}
		public Shader CameraShader
		{
			get
			{
				if (CameraMaterial == null)
					return null;
				else
					return CameraMaterial.Shader;
			}
		}
		public Vector3 PositionTarget { get; set; }
		public Quaternion RotationTarget { get; set; }
		public float TransAccel { get; set; }
		public float RotAccel { get; set; }
		public bool LerpRotation { get; set; }
		public bool LerpTranslation { get; set; }
		public bool PitchLock { get; set; }
		
		
		public float ZNear
		{
			get
			{
				return zNear;
			}
			set
			{
				zNear = value;
				SetupProjection();
			}
		}
		
		public float ZFar
		{
			get
			{
				return zFar;
			}
			set
			{
				zFar = value;
				SetupProjection();
			}
		}
		
		public float Fov
		{
			get
			{
				return fov;
			}
			set
			{
				fov = value;
				SetupProjection();
			}
		}
		public Vector2 NearplaneSize
		{
			get
			{
				float h = 2 * Mathf.Tan(Mathf.Deg2Rad(Fov) / 2) * ZNear;
				return new Vector2(h * AspectRatio, h);
			}
		}
		public float AspectRatio { get; private set; }
		internal override void Init()
		{
			RotationTarget = Quaternion.Identity;
			LerpRotation = false;
			LerpTranslation = true;
			TransAccel = 1;
			RotAccel = 1;
			PitchLock = true;
			fov = 90;
			zNear = 0.2f;
			zFar = 120f;
			screenMesh = new Mesh();
			screenMesh.SetVertices(new float[] {
				-1,-1,
				1,-1,
				1,1,
				-1,1
			});
			screenMesh.SetIndices(new uint[] {
				2,3,0,
				0,1,2
			});
			screenMesh.SetDrawHints(new VertexObjectDrawHint("pos", 2, 2, 0, false));
			screenMesh.UpdateBuffers();
			VAO = GL.GenVertexArray();
			SetupProjection();
		}
		public Matrix4 GetModelViewProjectionMatrix(Transform model)
		{
			Matrix4 m = model.GetMatrix();
			Matrix4 v = Transform.GetMatrix().Inverted();
			Matrix4 p = projectionMatrix;
			return m * v * p;
		}
		public void Update(float tDelta)
		{
			if(LerpTranslation)
				Transform.LocalPosition += (PositionTarget - Transform.LocalPosition) * TransAccel * tDelta;
			if (LerpRotation)
			{
				Transform.LocalRotation = Quaternion.Slerp(Transform.LocalRotation, RotationTarget, RotAccel * tDelta);
				
			}
		}
		public void MoveForward(float amount)
		{
			if (LerpTranslation)
			{
				TranslateTargetPosition(Transform.Forward * amount);
			}
			else
			{
				Transform.Translate(Transform.Forward * amount);
			}
		}
		public void MoveRight(float amount)
		{
			if (LerpTranslation)
			{
				TranslateTargetPosition(Transform.Right * amount);
			}
			else
			{
				Transform.Translate(Transform.Right * amount);
			}
		}
		public void MoveUp(float amount)
		{
			if (LerpTranslation)
			{
				TranslateTargetPosition(Transform.Up* amount);
			}
			else
			{
				Transform.Translate(Transform.Up * amount);
			}
		}
		public void Translate(Vector3 amount)
		{
			if (LerpTranslation)
			{
				TranslateTargetPosition(amount);
			}
			else
			{
				Transform.Translate(amount);
			}
		}
		private void TranslateTargetPosition(Vector3 amount)
		{
			PositionTarget += amount;
		}
		
		public void Rotate(Vector3 axis, float angle)
		{
			
			Transform.Rotate(axis, angle);
			if (PitchLock)
			{
				float a = Vector3.CalculateAngle(Vector3.UnitY, Transform.Up);
				float sign =  -Math.Sign(Transform.Forward.Y);
				float delta = a - (float)Math.PI / 2;
				if (delta > 0)
					Transform.Rotate(Transform.Right, delta * sign);
			}
		}
		public void SetCameraShader(Shader shader, int multisampling)
		{
			if (CameraMaterial != null)
			{
				CameraMaterial.Dispose();
				CameraMaterial = null;
			}
			if(shader != null)
			{
				Surface bufferSurface;
				if (multisampling > 1)
				{
					if (multisampler != null)
						multisampler.Dispose();
					multisampler = new Surface(GameObject.App.Window.Width, GameObject.App.Window.Height, new SurfaceFormat { WrapMode = TextureWrapMode.Clamp, Multisampling = multisampling, DepthBuffer = true });
					bufferSurface = new Surface(GameObject.App.Window.Width, GameObject.App.Window.Height, new SurfaceFormat { WrapMode = TextureWrapMode.Clamp, DepthBuffer = false });
				}
				else
				{
					if (multisampler != null)
						multisampler.Dispose();
					multisampler = null;
					bufferSurface = new Surface(GameObject.App.Window.Width, GameObject.App.Window.Height, new SurfaceFormat { WrapMode = TextureWrapMode.Clamp, DepthBuffer = true });
				}
				CameraMaterial = new Material(shader);
				CameraMaterial.AddTexture("_tex", bufferSurface);
			}
		}
		private void SetupProjection()
		{
			if(GameObject != null)
			{
				AspectRatio = GameObject.App.Window.Width / (float)GameObject.App.Window.Height;
				projectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)((Math.PI * Fov) / 180), AspectRatio, ZNear, ZFar);
			}
		}
		public void BeginDraw()
		{
			if(CameraMaterial != null)
			{
				if (multisampler != null)
				{
					multisampler.Clear();
					if(App.MeshRenderCore.UseAlphaToCoverage)
						GL.Enable(EnableCap.SampleAlphaToCoverage);
					multisampler.BindFramebuffer();
				}
				else
				{
					CameraMaterial.Textures["_tex"].Clear();
					CameraMaterial.Textures["_tex"].BindFramebuffer();
				}
				beganDraw = true;
			}
		}
		public void EndDraw()
		{
			if (beganDraw)
			{
				beganDraw = false;
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
				
				if (CameraMaterial != null)
				{
					if (multisampler != null)
					{
						GL.Disable(EnableCap.SampleAlphaToCoverage);
						var camMat = CameraMaterial.Textures["_tex"];
						multisampler.BindFramebuffer(FramebufferTarget.ReadFramebuffer);
						camMat.BindFramebuffer(FramebufferTarget.DrawFramebuffer);
						GL.BlitFramebuffer(0,0,multisampler.Width, multisampler.Height, 0,0,camMat.Width, camMat.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
						GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
						GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
					}
					GL.BindBuffer(BufferTarget.ArrayBuffer, screenMesh.VBO);
					GL.BindVertexArray(VAO);
					GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenMesh.VEO);
					CameraMaterial.Use();
					screenMesh.ApplyDrawHints(CameraMaterial.Shader);
					GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
					GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
					GL.BindVertexArray(0);
					GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
					GL.UseProgram(0);
				}
			}
		}
		public Vector2 WorldToScreen(Vector3 world)
		{
			/*Vector3 tmp = Vector3.Transform(world, (GameObject.Transform.GetMatrix() * projectionMatrix));
			tmp.X = (tmp.X + 1) * 0.5f * GameObject.App.Window.Width;
			tmp.Y = (1 - tmp.Y) * 0.5f * GameObject.App.Window.Height;
			return new Vector2(tmp.X, tmp.Y);*/
			throw (new NotImplementedException());
		}
		
		public Vector3 ScreenToDirection(Vector2 screen)
		{
			Vector2 ss = new Vector2(GameObject.App.Window.Width, -GameObject.App.Window.Height);
			screen.X /= ss.X;
			screen.Y /= ss.Y;
			screen -= new Vector2(0.5f, -0.5f);
			screen.Y /= AspectRatio;
			screen *= NearplaneSize.X;
			return Vector3.Transform(new Vector3(screen.X, screen.Y, -ZNear).Normalized(), Transform.Rotation);
		}
		public Vector3 ScreenToWorld(Vector2 screen)
		{
			return GameObject.Transform.LocalPosition + ScreenToDirection(screen);
		}
	}
}
