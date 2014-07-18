﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using SharpGL.Drawing;
using SharpGL.Input;
using SharpGL.Components;
using SharpGL.Factories;
namespace SharpGL
{
	public class App
	{
		/// <summary>
		/// A dictionary to store shaders (contains default shaders, list dictionary keys for the names)
		/// </summary>
		public Dictionary<string, Shader> Shaders;
		/// <summary>
		/// A dictionary to store materials (contains default materials, list dictionary keys for the names)
		/// </summary>
		public Dictionary<string, Material> Materials;
		/// <summary>
		/// Factory to create basic game objects
		/// </summary>
		public GameObjectFactory GameObjectFactory { get; protected set; }
		/// <summary>
		/// Factory to create / get primitive meshes
		/// </summary>
		public PrimitiveFactory PrimitiveFactory { get; protected set; }
		/// <summary>
		/// Rendering core. Renders registered MeshRenderers grouped by Mesh -> Material
		/// </summary>
		public MeshRenderCore MeshRenderCore { get; protected set; }
		private Dictionary<string, GameObject> GameObjects;
		
		private System.Diagnostics.Stopwatch stopWatch;
		private System.Diagnostics.Stopwatch time;
		/// <summary>
		/// Gets the current game time in seconds
		/// </summary>
		public float Time
		{
			get
			{
				return time.ElapsedMilliseconds / 1000f;
			}
		}
		/// <summary>
		/// OpenTK Window instance
		/// </summary>
		public  GameWindow Window { get; private set; }
		/// <summary>
		/// Window background color
		/// </summary>
        protected Color BackgroundColor { get; set; }

		/// <summary>
		/// returns the Gameobject of the ActiveCamera
		/// </summary>
		public GameObject CameraContainer
		{
			get
			{
				if (ActiveCamera == null)
					return null;
				return ActiveCamera.GameObject;
			}
		}
		/// <summary>
		/// Stores the active camera. By default this camera is passed to the MeshRendererCore
		/// </summary>
		public Camera ActiveCamera { get; set; }
		/// <summary>
		/// Delta time of the current update call
		/// </summary>
		public float DT { get; private set; }
        public App(int width, int height)
        {
			GameObjects = new Dictionary<string, GameObject>();
			Shaders = new Dictionary<string, Shader>();
			Materials = new Dictionary<string, Material>();

            Window = new GameWindow(width, height, new GraphicsMode(32, 24,0, 4));
            Window.Load += OnLoadInternal;
            Window.Resize += OnResizeInternal;
            Window.UpdateFrame += OnUpdateInternal;
            Window.RenderFrame += OnRenderInternal;
			MeshRenderCore = new SharpGL.MeshRenderCore();
			SetupGL();
			
			var cameraContainer = CreateGameObject("Camera");
			ActiveCamera = cameraContainer.AddComponent<Camera>();
			ActiveCamera.TransAccel = 4f;
			stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();
			time = new System.Diagnostics.Stopwatch();
			time.Start();

			Shaders.Add("unlit", new Shader("Shaders/unlit.glsl", "vertex", null, "fragment"));
			Shaders.Add("screen", new Shader("Shaders/screen.glsl", "vertex", null, "fragment"));
			Shaders.Add("screenCA", new Shader("Shaders/chromaticAbberation.glsl", "vertex", null, "fragment"));

			Materials.Add("unlit", new Material(Shaders["unlit"]));

			GameObjectFactory = new GameObjectFactory(this);
			PrimitiveFactory = new PrimitiveFactory();
			Window.Run(60.0);
			
        }
		/// <summary>
		/// Creates a new empty game object
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
        public GameObject CreateGameObject(string name)
		{
			int  i = 2;
			string newName = name;
			while(GameObjects.ContainsKey(newName))
			{
				newName = name + i;
				i++;
			}
			var go = new GameObject(newName, this);
			GameObjects.Add(newName, go);
			return go;
		}
		
        private void OnRenderInternal(object sender, FrameEventArgs e)
        {
            var window = (GameWindow)sender;
			
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			if(ActiveCamera != null)
				ActiveCamera.BeginDraw();
			/*foreach(var go in GameObjects.Values)
			{
				go.Render(Time);
			}*/
			MeshRenderCore.Render(ActiveCamera, Time);
			//User drawing
           // OnDraw();
			if (ActiveCamera != null)
				ActiveCamera.EndDraw();
            window.SwapBuffers();
			
        }
		
		
        private void OnUpdateInternal(object sender, FrameEventArgs e)
        {
			DT = stopWatch.ElapsedMilliseconds / 1000.0f;
			stopWatch.Restart();
			ActiveCamera.Update(DT);
			MouseHandler.Update();
            KeyboardHandler.Update();
            OnUpdate();
        }

        private void OnResizeInternal(object sender, EventArgs e)
        {
            var window = (GameWindow)sender;
        }

        private void OnLoadInternal(object sender, EventArgs e)
        {
            var window = (GameWindow)sender;
            KeyboardHandler.Init(window);
			MouseHandler.Init(window);
            OnLoad();
        }
		private void SetupGL()
		{
			GL.Enable(EnableCap.CullFace);
			EnabledTextureBlending();
			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask(true);
			GL.DepthFunc(DepthFunction.Less);
			GL.DepthRange(0.0f, 1.0f);
		}
		private void EnabledTextureBlending()
		{
			GL.Enable(EnableCap.Texture2D);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Enable(EnableCap.Blend);
		}
        /// <summary>
        /// Called when the game has finished initializing
        /// </summary>
        public virtual void OnLoad() { }
		/// <summary>
		/// Called every update
		/// </summary>
        public virtual void OnUpdate() { }

    }
}
