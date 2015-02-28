﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
namespace SharpGL.Input
{
    /// <summary>
    /// Manages key presses on the keyboard
    /// </summary>
    public static class KeyboardHandler
    {
        private static Dictionary<Key, bool> pressedKeys;
        private static Dictionary<Key, Action> keyDownActions;
		private static Dictionary<Key, Action> keyPressActions;
		private static Dictionary<Key, Action> keyReleaseActions;
        internal static void Init(GameWindow window)
        {
            window.KeyDown += window_KeyDown;
            window.KeyUp += window_KeyUp;
            pressedKeys = new Dictionary<Key, bool>();
            keyDownActions = new Dictionary<Key, Action>();
			keyPressActions = new Dictionary<Key, Action>();
			keyReleaseActions = new Dictionary<Key, Action>();
        }
        /// <summary>
        /// Registers an action to be called if a key is pressed or held down.
        /// </summary>
        /// <param name="k">The key</param>
        /// <param name="action">The action to be called</param>
        public static void RegisterKeyDown(Key k, Action action)
        {
			RegisterKey(k);
            keyDownActions.Add(k, action);
        }
        /// <summary>
        /// Registers an action to be called when a key is pressed.
        /// </summary>
        /// <param name="k">The key</param>
        /// <param name="action">The action to be called</param>
		public static void RegisterKeyPress(Key k, Action action)
		{
			RegisterKey(k);
			keyPressActions.Add(k, action);
		}
        /// <summary>
        /// Registers an action to be called when a key is released.
        /// </summary>
        /// <param name="k">The key</param>
        /// <param name="action">The action to be called</param>
		public static void RegisterKeyRelease(Key k, Action action)
		{
			RegisterKey(k);
			keyReleaseActions.Add(k, action);
		}
		private static void RegisterKey(Key k)
		{
			if (!pressedKeys.ContainsKey(k))
				pressedKeys.Add(k, false);
		}
        private static void window_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            if (pressedKeys.ContainsKey(e.Key))
            {
                pressedKeys[e.Key] = false;
            }
        }

        private static void window_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            if(pressedKeys.ContainsKey(e.Key))
            {
                pressedKeys[e.Key] = true;
            }
        }
        internal static void Update()
        {
            foreach(Key k in keyDownActions.Keys)
            {
                if (pressedKeys[k])
                    keyDownActions[k]();
            }
        }
    }
}
