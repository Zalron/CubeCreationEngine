using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay {

	public delegate bool OnConsoleEvent (string text);


	public partial class VoxelPlayUI : MonoBehaviour {
								
		/// <summary>
		/// Triggered when a new message is printed to the console
		/// </summary>
		public event OnConsoleEvent OnConsoleNewMessage;

		/// <summary>
		/// Triggered when a new command is entered by the user
		/// </summary>
		public event OnConsoleEvent OnConsoleNewCommand;

		/// <summary>
		/// Returns true if the console is visible
		/// </summary>
		public bool IsVisible {
			get {
				if (console != null)
					return console.activeSelf;
				return false;
			}
		}

		[SerializeField]
		int _inventoryRows = 10;

		public int inventoryRows {
			get { return _inventoryRows; }
			set {
				if (_inventoryRows != value) {
					_inventoryRows = Mathf.Clamp (value, 1, 10);
				}
			}
		}

		[SerializeField]
		int _inventoryColumns = 3;

		public int inventoryColumns {
			get { return _inventoryColumns; }
			set {
				if (_inventoryColumns != value) {
					_inventoryColumns = Mathf.Clamp (value, 1, 3);
				}
			}
		}

		[NonSerialized]
		public VoxelPlayEnvironment env;

		[NonSerialized]
		public bool inventoryUIShouldBeRebuilt;


		const string UI_CANVAS_NAME = "Voxel Play UI Canvas";
		static char[] SEPARATOR_SPACE = new char[] { ' ' };
		string KEY_CODES = "1234567890";

		StringBuilder sb, sbDebug;
		GameObject canvas, console, status, debug;
		RawImage selectedItem;
		GameObject selectedItemPlaceholder;
		Text consoleText, debugText, statusText, selectedItemName, selectedItemNameShadow, selectedItemQuantityShadow, selectedItemQuantity, inventoryTitleText, fpsText, fpsShadow;
		GameObject inventoryPlaceholder, inventoryItemTemplate, inventoryTitle, initPanel;
		Transform initProgress;
		RectTransform rtCanvas;
		string lastCommand;

		static VoxelPlayUI _console;
		InputField inputField;
		bool firstTimeConsole;
		bool firstTimeInventory;
		char[] forbiddenCharacters = new char[]{ '<', '>' };
		List<GameObject> inventoryItems;
		List<RawImage> inventoryItemsImages;
		int inventoryCurrentPage;
		Image statusBackground;
		int columnToShow = 0;
		bool leftShiftPressed;

		float fpsUpdateInterval = 0.5f;
		float fpsAccum = 0;
		// FPS accumulated over the interval
		int fpsFrames = 0;
		// Frames drawn over the interval
		float fpsTimeleft;
		// Left time for current interval

		public static VoxelPlayUI instance {
			get {
				if (_console == null) {
					_console = VoxelPlayEnvironment.instance.GetComponent<VoxelPlayUI> ();
					if (_console == null) {
						_console = VoxelPlayEnvironment.instance.gameObject.AddComponent<VoxelPlayUI> ();
					}
				}
				return _console;
			}
		}


		void OnEnable () {
			InitUI ();
		}

		void InitUI () {
			firstTimeConsole = true;
			firstTimeInventory = true;
			inventoryCurrentPage = 0;
			lastCommand = "";
			CheckReferences ();
			fpsTimeleft = fpsUpdateInterval;  
			fpsFrames = 1000;
		}

		void CheckReferences () {
			if (env == null) {
				env = VoxelPlayEnvironment.instance;
			}

			sb = new StringBuilder (1000);
			sbDebug = new StringBuilder (1000);

			if (canvas == null) {
				if (env.UICanvasPrefab == null) {
					Debug.LogError ("Voxel Play: UI Canvas not defined.");
					return;
				} else {
					canvas = GameObject.Find (UI_CANVAS_NAME);
					if (canvas == null) {
						canvas = Instantiate<GameObject> (env.UICanvasPrefab); 
						canvas.name = UI_CANVAS_NAME;
						canvas.transform.SetParent (env.worldRoot, false);
					}
				}
			}
			CheckEventSystem ();
			rtCanvas = canvas.GetComponent<RectTransform> ();
			selectedItemPlaceholder = canvas.transform.Find ("ItemPlaceholder").gameObject;
			selectedItem = selectedItemPlaceholder.transform.Find ("ItemImage").GetComponent<RawImage> ();
			selectedItemName = selectedItemPlaceholder.transform.Find ("ItemName").GetComponent<Text> ();
			selectedItemNameShadow = selectedItemPlaceholder.transform.Find ("ItemNameShadow").GetComponent<Text> ();
			selectedItemQuantity = selectedItemPlaceholder.transform.Find ("QuantityShadow/QuantityText").GetComponent<Text> ();
			selectedItemQuantityShadow = selectedItemPlaceholder.transform.Find ("QuantityShadow").GetComponent<Text> ();
			fpsShadow = canvas.transform.Find ("FPSShadow").GetComponent<Text> ();
			fpsText = fpsShadow.transform.Find ("FPSText").GetComponent<Text> ();
			fpsShadow.gameObject.SetActive (env.showFPS);
			console = canvas.transform.Find ("Console").gameObject;
			console.GetComponent<Image> ().color = env.consoleBackgroundColor;
			consoleText = canvas.transform.Find ("Console/Scroll View/Viewport/ConsoleText").GetComponent<Text> ();
			status = canvas.transform.Find ("Status").gameObject;
			statusBackground = status.GetComponent<Image> ();
			statusBackground.color = env.statusBarBackgroundColor;
			statusText = canvas.transform.Find ("Status/StatusText").GetComponent<Text> ();
			debug = canvas.transform.Find ("Debug").gameObject;
			debug.GetComponent<Image> ().color = env.consoleBackgroundColor;
			debugText = canvas.transform.Find ("Debug/Scroll View/Viewport/DebugText").GetComponent<Text> ();
			inputField = canvas.transform.Find ("Status/InputField").GetComponent<InputField> ();
			inputField.onEndEdit.AddListener (delegate {
				UserConsoleCommandHandler ();
			});
			inventoryPlaceholder = canvas.transform.Find ("InventoryPlaceholder").gameObject;
			inventoryItemTemplate = inventoryPlaceholder.transform.Find ("ItemButtonTemplate").gameObject;
			inventoryTitle = inventoryPlaceholder.transform.Find ("Title").gameObject;
			inventoryTitleText = inventoryPlaceholder.transform.Find ("Title/Text").GetComponent<Text> ();
			inventoryUIShouldBeRebuilt = true;
			initPanel = canvas.transform.Find ("InitPanel").gameObject;
			initProgress = initPanel.transform.Find ("Box/Progress").transform;
		}

		void OnDisable () {
			inputField.onEndEdit.RemoveAllListeners ();
		}

		void LateUpdate () {
			VoxelPlayInputController input = env.input;
			if (input == null || inventoryPlaceholder == null)
				return;
			if (input.anyKey) {
				if (env.enableConsole && Input.GetKeyDown (KeyCode.F1)) {
					ToggleConsoleVisibility (!console.activeSelf);
				} else if (env.enableDebugWindow && Input.GetKeyDown (KeyCode.F2)) {
					ToggleDebugWindow (!debug.activeSelf);
				} else if (Input.GetKeyDown (KeyCode.Escape)) {
					ToggleConsoleVisibility (false);
					ToggleInventoryVisibility (false);
				} else if (env.enableInventory && input.GetButtonDown (InputButtonNames.Inventory)) { 
					leftShiftPressed = false;
					if (!inventoryPlaceholder.activeSelf) {
						ToggleInventoryVisibility (true);
					} else if (Input.GetKey (KeyCode.LeftShift)) {
						InventoryPreviousPage ();
					} else {
						InventoryNextPage ();
					}
				} else if (Input.GetKeyDown (KeyCode.UpArrow) && IsVisible) {
					inputField.text = lastCommand;
					inputField.MoveTextEnd (false);
				} else if (Input.GetKeyDown (KeyCode.F8)) {
					ToggleFPS ();
				} else if (inventoryPlaceholder.activeSelf) {
					if (Input.GetKeyDown (KeyCode.Alpha1)) {
						SelectItemFromVisibleInventorySlot (0);
					} else if (Input.GetKeyDown (KeyCode.Alpha2)) {
						SelectItemFromVisibleInventorySlot (1);
					} else if (Input.GetKeyDown (KeyCode.Alpha3)) {
						SelectItemFromVisibleInventorySlot (2);
					} else if (Input.GetKeyDown (KeyCode.Alpha4)) {
						SelectItemFromVisibleInventorySlot (3);
					} else if (Input.GetKeyDown (KeyCode.Alpha5)) {
						SelectItemFromVisibleInventorySlot (4);
					} else if (Input.GetKeyDown (KeyCode.Alpha6)) {
						SelectItemFromVisibleInventorySlot (5);
					} else if (Input.GetKeyDown (KeyCode.Alpha7)) {
						SelectItemFromVisibleInventorySlot (6);
					} else if (Input.GetKeyDown (KeyCode.Alpha8)) {
						SelectItemFromVisibleInventorySlot (7);
					} else if (Input.GetKeyDown (KeyCode.Alpha9)) {
						SelectItemFromVisibleInventorySlot (8);
					} else if (Input.GetKeyDown (KeyCode.Alpha0)) {
						SelectItemFromVisibleInventorySlot (9);
					}
				}
			}

			if (inventoryPlaceholder.activeSelf) {
				CheckInventoryControlKeyHints (true);
			}

			if (debug.activeSelf) {
				UpdateDebugInfo ();
			}

			if (fpsText.enabled) {
				UpdateFPSCounter ();
			}

		}


		void CheckEventSystem () {
			EventSystem eventSystem = GameObject.FindObjectOfType<EventSystem> ();
			if (eventSystem == null) {
				GameObject prefab = Resources.Load<GameObject> ("VoxelPlay/Prefabs/EventSystem");
				if (prefab != null) {
					GameObject go = Instantiate (prefab) as GameObject;
					go.name = "EventSystem";
				}
			}
		}

		void EnableCursor (bool state) {
			if (env.initialized) {
				VoxelPlayFirstPersonController controller = VoxelPlayFirstPersonController.instance;
				if (controller != null) {
					controller.mouseLook.SetCursorLock (!state);
					controller.enabled = !state;
				}
			}
		}

		#region Console

		void PrintKeySheet () {
			if (sb.Length > 0) {
				sb.AppendLine ();
				sb.AppendLine ();
			}
			sb.AppendLine ("<color=orange>** KEY LIST **</color><");
			AppendValue ("W/A/S/D");
			sb.AppendLine (" : Move player (front/left/back/right)");
			AppendValue ("F");
			sb.AppendLine (" : Toggle Flight Mode");
			AppendValue ("Q/E");
			sb.AppendLine (" : Fly up / down");
			AppendValue ("C");
			sb.AppendLine (" : Toggles crouching");
			AppendValue ("Left Shift");
			sb.AppendLine (" : Hold while move to run / fly faster");
			AppendValue ("T");
			sb.AppendLine (" : Interacts with an object");
			AppendValue ("G");
			sb.AppendLine (" : Throws currently selected item");
			AppendValue ("L");
			sb.AppendLine (" : Toggles character light");
			AppendValue ("Mouse Move");
			sb.AppendLine (" : Look around");
			AppendValue ("Mouse Left Button");
			sb.AppendLine (" : Fire / hit blocks");
			AppendValue ("Mouse Right Button");
			sb.AppendLine (" : Build blocks");
			AppendValue ("Tab");
			sb.AppendLine (" : Show inventory and browse items (Tab / Shift-Tab)");
			AppendValue ("Esc");
			sb.AppendLine (" : Closes all windows (inventory, console)");
			AppendValue ("B");
			sb.AppendLine (" : Activate Build mode");
			AppendValue ("F1");
			sb.AppendLine (" : Show / hide console");
			AppendValue ("F2");
			sb.AppendLine (" : Show / hide debug window");
			AppendValue ("Control + F3");
			sb.Append (" : Load Game / ");
			AppendValue ("Control + F4");
			sb.AppendLine (" : Quick save");
			#if UNITY_EDITOR
			AppendValue ("F5");
			sb.Append (" : Load Model / ");
			AppendValue ("F6");
			sb.AppendLine (" : Save Model (only in Constructor)");
			#endif
			AppendValue ("F8");
			sb.Append (" : Toggle FPS");
			consoleText.text = sb.ToString ();
		}

		void PrintCommands () {
			if (sb.Length > 0) {
				sb.AppendLine ();
				sb.AppendLine ();
			}											
			sb.AppendLine ("<color=orange>** COMMAND LIST **</color>");
			AppendValue ("/help");
			sb.AppendLine (" : Show this list of commands");
			AppendValue ("/keys");
			sb.AppendLine (" : Show available keys and actions");
			AppendValue ("/clear");
			sb.AppendLine (" : Clear the console");
			AppendValue ("/invoke GameObject MethodName");
			sb.AppendLine (" : Call method 'MethodName' on target GameObject");
			AppendValue ("/save filename");
			sb.AppendLine (" : Save current game to 'filename' (only filename, no extension)");
			AppendValue ("/load filename");
			sb.AppendLine (" : Load a previously saved game");
			AppendValue ("/build");
			sb.AppendLine (" : Enable/disable build mode (hotkey: <color=yellow>B</color>)");
			AppendValue ("/teleport x y z");
			sb.AppendLine (" : Instantly teleport player to x y z location");
			AppendValue ("/stuck");
			sb.AppendLine (" : Moves player on top of ground");
			AppendValue ("/inventory rows columns");
			sb.AppendLine (" : Changes inventory panel size");
			AppendValue ("/viewDistance dist");
			sb.AppendLine (" : Sets visible chunk distance (2-20)");
			AppendValue ("/flood on/off");
			sb.AppendLine (" : Toggles water flood");
			AppendValue ("/debug");
			sb.AppendLine (" : Shows debug info about the last voxel hit");
			sb.Append ("Press <color=yellow>F1</color> again or <color=yellow>ESC</color> to return to game.");

			consoleText.text = sb.ToString ();
		}

		void AppendValue (object o) {
			sb.Append ("<color=yellow>");
			sb.Append (o);
			sb.Append ("</color>");
		}


		/// <summary>
		/// Shows/hides the console
		/// </summary>
		/// <param name="state">If set to <c>true</c> state.</param>
		public void ToggleConsoleVisibility (bool state) {
			if (!env.applicationIsPlaying)
				return;

			if (statusText == null) {
				CheckReferences ();
				if (statusText == null)
					return;
			}

			if (firstTimeConsole) {
				firstTimeConsole = false;
				AddConsoleText ("<color=green>Enter <color=yellow>/help</color> for a list of commands.</color>");
			}
			status.SetActive (state);
			console.SetActive (state);
			consoleText.fontSize = statusText.fontSize;

			EnableCursor (state);

			if (state) {
				ToggleInventoryVisibility (false);
				statusText.text = "";
				FocusInputField ();
			}

			VoxelPlayEnvironment.instance.input.enabled = !state;
		}

		/// <summary>
		/// Adds a custom text to the console
		/// </summary>
		public void AddConsoleText (string text) {
			if (sb == null || consoleText == null || !env.enableStatusBar)
				return;
			if (sb.Length > 0) {
				sb.AppendLine ();
			}
			if (sb.Length > 12000) {
				sb.Length = 0;
			}
			sb.Append (text);
			consoleText.text = sb.ToString ();
		}

		/// <summary>
		/// Adds a custom message to the status bar and to the console.
		/// </summary>
		public void AddMessage (string text, float displayTime = 4f, bool flash = true) {
			if (!Application.isPlaying || !env.enableStatusBar)
				return;

			if (statusText == null) {
				CheckReferences ();
				if (statusText == null)
					return;
			}

			if (text != statusText.text) {
				AddConsoleText (text);

				// If console is not shown, only show this message
				if (!console.activeSelf) {
					statusText.text = text;
					status.SetActive (true);
					CancelInvoke ("HideStatusText");
					Invoke ("HideStatusText", displayTime);
					if (flash) {
						StartCoroutine (FlashStatusText ());
					}
				}
				if (OnConsoleNewMessage != null) {
					OnConsoleNewMessage (text);
				}
			}
		}

		IEnumerator FlashStatusText () {
			if (statusBackground == null)
				yield break;
			float startTime = Time.time;
			float elapsed;
			Color startColor = new Color (0, 1.1f, 1.1f, env.statusBarBackgroundColor.a);
			do {
				elapsed = Time.time - startTime;
				if (elapsed >= 1f)
					elapsed = 1f;
				statusBackground.color = Color.Lerp (startColor, env.statusBarBackgroundColor, elapsed);
				yield return  new WaitForEndOfFrame ();
			} while(elapsed < 1f);
		}


		/// <summary>
		/// Hides the status bar
		/// </summary>
		public void HideStatusText () {
			statusText.text = "";
			if (console.activeSelf)
				return;
			status.SetActive (false);
		}

		void UserConsoleCommandHandler () {
			string text = inputField.text;
			bool sanitize = false;
			for (int k = 0; k < forbiddenCharacters.Length; k++) {
				if (text.IndexOf (forbiddenCharacters [k]) >= 0) {
					sanitize = true;
					break;
				}
			}
			if (sanitize) {
				string[] temp = text.Split (forbiddenCharacters, StringSplitOptions.RemoveEmptyEntries);
				text = String.Join ("", temp);
			}

			if (!string.IsNullOrEmpty (text)) {
				lastCommand = text;
				bool captured = false;
				if (OnConsoleNewCommand != null) {
					captured = OnConsoleNewCommand (inputField.text);
				}
				if (!captured && !ProcessConsoleCommand (text)) {
					env.ShowMessage (text);
				}
				inputField.text = "";
				FocusInputField (); // avoids losing focus
			}
		}

		void FocusInputField () {
			inputField.ActivateInputField ();
			inputField.Select ();
		}

		bool ProcessConsoleCommand (string command) {
			string upperCommand = command.ToUpper ();
			if (upperCommand.IndexOf ("/CLEAR") >= 0) {
				sb.Length = 0;
				consoleText.text = "";
				return true;
			} else if (upperCommand.IndexOf ("/KEYS") >= 0) {
				PrintKeySheet ();
				return true;
			} else if (upperCommand.IndexOf ("/HELP") >= 0) {
				PrintCommands ();
				return true;
			} else if (upperCommand.IndexOf ("/INVOKE") >= 0) {
				ProcessInvokeCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/LOAD") >= 0) {
				ProcessLoadCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/SAVE") >= 0) {
				ProcessSaveCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/BUILD") >= 0) {
				ToggleConsoleVisibility (false);
				env.SetBuildMode (!env.buildMode);
				return true;
			} else if (upperCommand.IndexOf ("/TELEPORT") >= 0) {
				ToggleConsoleVisibility (false);
				ProcessTeleportCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/INVENTORY") >= 0) {
				ProcessInventoryResizeCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/VIEWDISTANCE") >= 0) {
				ProcessViewDistanceCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/DEBUG") >= 0) {
				ToggleDebugWindow (!debug.activeSelf);
				return true;
			} else if (upperCommand.IndexOf ("/FLOOD") >= 0) {
				ProcessFloodCommand (command);
				return true;
			} else if (upperCommand.IndexOf ("/STUCK") >= 0) {
				if (VoxelPlayFirstPersonController.instance != null)
					VoxelPlayFirstPersonController.instance.Unstuck (true);
				return true;
			}
			return false;
		}

		void ProcessInvokeCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 3) {
				string goName = args [1];
				string cmdParams = args [2];
				GameObject go = GameObject.Find (goName);
				if (go == null) {
					AddMessage ("GameObject '" + goName + "' not found.");
				} else {
					go.SendMessage (cmdParams, SendMessageOptions.DontRequireReceiver);
					ToggleConsoleVisibility (false);
				}
			}
		}

		void ProcessSaveCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2) {
				env.saveFilename = args [1];
				env.SaveGameBinary ();
			}
		}

		void ProcessLoadCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2) {
				env.saveFilename = args [1];
				if (!env.LoadGameBinary (false)) {
					AddMessage ("<color=red>Load error:</color><color=orange> Game '<color=white>" + args [1] + "</color>' could not be loaded.</color>");
				}
			}
		}

		void ProcessFloodCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2) {
				string mode = args [1].ToUpper ();
				env.enableWaterFlood = "ON".Equals (mode);
			}
			AddMessage ("<color=green>Flood is <color=yellow>" + (env.enableWaterFlood ? "ON" : "OFF") + "</color></color>");
		}

		void ProcessTeleportCommand (string command) {
			try {
				string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
				if (args.Length >= 3) {
					float x = float.Parse (args [1]);
					float y = float.Parse (args [2]);
					float z = float.Parse (args [3]);
					env.characterController.transform.position = new Vector3 (x + 0.5f, y, z + 0.5f);
					ToggleConsoleVisibility (false);
				}
			} catch {
				AddInvalidCommandError ();
			}
		}

		void ProcessInventoryResizeCommand (string command) {
			try {
				string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
				if (args.Length >= 2) {
					int rows = int.Parse (args [1]);
					int columns = int.Parse (args [2]);
					if (rows > 0 && columns > 0) {
						inventoryRows = rows;
						inventoryColumns = columns;
						inventoryUIShouldBeRebuilt = true;
						ToggleInventoryVisibility (true);
					}
				}
			} catch {
				AddInvalidCommandError ();
			}
		}


		void ProcessViewDistanceCommand (string command) {
			try {
				string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
				if (args.Length >= 1) {
					int distance = int.Parse (args [1]);
					if (distance >= 2 && distance <= 20) {
						env.visibleChunksDistance = distance;
					}
				}
			} catch {
				AddInvalidCommandError ();
			}
		}




		void AddInvalidCommandError () {
			AddMessage ("<color=orange>Invalid command.</color>");
		}

		#endregion

		#region Inventory related

		/// <summary>
		/// Show/hide inventory
		/// </summary>
		/// <param name="visible">If set to <c>true</c> visible.</param>
		public void ToggleInventoryVisibility (bool state) {
			if (!state) {
				inventoryPlaceholder.SetActive (false);
			} else {
				CheckInventoryUI ();
				RefreshInventoryContents ();
				inventoryPlaceholder.SetActive (true);
				if (firstTimeInventory) {
					firstTimeInventory = false;
					if (!env.isMobilePlatform) {
						env.ShowMessage ("<color=green>Press <color=yellow>Number</color> to select an item, <color=yellow>Shift</color> to toggle column.</color>");
					}
				}
			}
			ToggleSelectedItemName ();
		}

		/// <summary>
		/// Advances to next inventory page
		/// </summary>
		public void InventoryNextPage () {
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			if ((inventoryCurrentPage + 1) * itemsPerPage < VoxelPlayPlayer.instance.items.Count) {
				inventoryCurrentPage++;
				RefreshInventoryContents ();
			} else {
				inventoryCurrentPage = 0;
				ToggleInventoryVisibility (false);
			}
		}

		/// <summary>
		/// Shows previous inventory page
		/// </summary>
		void InventoryPreviousPage () {
			if (inventoryCurrentPage > 0) {
				inventoryCurrentPage--;
			} else {
				int itemsPerPage = _inventoryRows * _inventoryColumns;
				inventoryCurrentPage = VoxelPlayPlayer.instance.items.Count / itemsPerPage;
			}
			RefreshInventoryContents ();
		}

		/// <summary>
		/// Builds the inventory UI elements
		/// </summary>
		void CheckInventoryUI () {

			const float itemSize = 48;
			const float padding = 3;

			float panelWidth = padding + _inventoryColumns * (itemSize + padding);
			float panelHeight;
			bool refit;
			do {
				refit = false;
				panelHeight = padding + _inventoryRows * (itemSize + padding);
				if (_inventoryRows > 3 && panelHeight * rtCanvas.localScale.y > Screen.height * 0.9f) {
					refit = true;
					inventoryUIShouldBeRebuilt = true;
					_inventoryRows--;
				}
			} while (refit); 

			if (!inventoryUIShouldBeRebuilt)
				return;
			Transform root = inventoryPlaceholder.transform.Find ("Root");
			if (root != null)
				DestroyImmediate (root.gameObject);
			GameObject rootGO = new GameObject ("Root");
			root = rootGO.transform;
			root.SetParent (inventoryPlaceholder.transform, false);


			if (inventoryItems == null)
				inventoryItems = new List<GameObject> ();
			else
				inventoryItems.Clear ();
												
			if (inventoryItemsImages == null)
				inventoryItemsImages = new List<RawImage> ();
			else
				inventoryItemsImages.Clear ();

			inventoryPlaceholder.GetComponent<RectTransform> ().sizeDelta = new Vector2 (panelWidth, panelHeight);
			int i = 0;
			for (int c = 0; c < _inventoryColumns; c++) {
				float x = padding + c * (itemSize + padding);
				for (int r = 0; r < _inventoryRows; r++) {
					float y = padding + r * (itemSize + padding);
					GameObject itemButton = Instantiate (inventoryItemTemplate) as GameObject;
					inventoryItems.Add (itemButton);
					itemButton.transform.SetParent (root, false);
					RectTransform rt = itemButton.GetComponent<RectTransform> ();
					rt.anchoredPosition = new Vector2 (x, panelHeight * 0.5f - y);
					itemButton.SetActive (true);
					string keyCode = r < KEY_CODES.Length ? KEY_CODES.Substring (r, 1) : "";
					Text t = itemButton.transform.Find ("KeyCodeShadow/KeyCodeText").GetComponent<Text> ();
					t.enabled = c == 0;
					t.text = keyCode;
					inventoryItemsImages.Add (itemButton.GetComponent<RawImage> ());
					int aux = i; // dummy assignation so the lambda expression takes the appropiate value and not always the last item
					itemButton.GetComponent<Button> ().onClick.AddListener (delegate() {
						InventoryImageClick (aux);
					});
					i++;
				}
			}
		}

		void CheckInventoryControlKeyHints (bool forceRefresh) {
			if (inventoryItems == null)
				return;

			bool refresh = false;
			if (Input.GetKeyDown (KeyCode.LeftShift)) {
				leftShiftPressed = true;
			}
			bool leftShiftReleased = Input.GetKeyUp (KeyCode.LeftShift);
			if (leftShiftReleased && leftShiftPressed) {
				leftShiftPressed = false;
				refresh = true;
				columnToShow++;
				if (columnToShow > 2)
					columnToShow = 0;
			}
			if (!refresh && !forceRefresh)
				return;

			List<InventoryItem> playerItems = VoxelPlayPlayer.instance.items;
			int playerItemsCount = playerItems != null ? playerItems.Count : 0;
			if (_inventoryRows * (columnToShow + 1) >= playerItemsCount) {
				columnToShow = 0;
			}

			int i = 0;
			for (int c = 0; c < _inventoryColumns; c++) {
				bool hintVisible = !env.isMobilePlatform && c == columnToShow;
				for (int r = 0; r < _inventoryRows; r++) {
					GameObject itemButton = inventoryItems [i];
					Image image = itemButton.transform.Find ("KeyCodeShadow").GetComponent<Image> ();
					image.enabled = hintVisible;
					Text t = itemButton.transform.Find ("KeyCodeShadow/KeyCodeText").GetComponent<Text> ();
					t.enabled = hintVisible;
					i++;
				}
			}
		}

		void InventoryImageClick (int inventoryImageIndex) {
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			int itemIndex = inventoryCurrentPage * itemsPerPage + inventoryImageIndex;
			VoxelPlayPlayer.instance.selectedItemIndex = itemIndex;
			ToggleInventoryVisibility (false);
		}

		/// <summary>
		/// Refreshs the inventory contents.
		/// </summary>
		public void RefreshInventoryContents () {
			if (inventoryItemsImages == null || env == null)
				return;
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			int selectedItemIndex = VoxelPlayPlayer.instance.selectedItemIndex;
			List<InventoryItem> playerItems = VoxelPlayPlayer.instance.items;
			int playerItemsCount = playerItems != null ? playerItems.Count : 0;
			if (inventoryCurrentPage * itemsPerPage > playerItemsCount) {
				inventoryCurrentPage = 0;
			}
			int inventoryItemsImagesCount = inventoryItemsImages.Count;
			for (int k = 0; k < itemsPerPage; k++) {
				int itemIndex = inventoryCurrentPage * itemsPerPage + k;
				if (k >= inventoryItemsImagesCount)
					continue;
				Text quantityShadow = inventoryItemsImages [k].transform.Find ("QuantityShadow").GetComponent<Text> ();
				Text quantityText = inventoryItemsImages [k].transform.Find ("QuantityShadow/QuantityText").GetComponent<Text> ();
				inventoryItemsImages [k].gameObject.SetActive (true);
				if (itemIndex < playerItemsCount) {
					inventoryItemsImages [k].color = Misc.colorWhite;
					inventoryItemsImages [k].texture = playerItems [itemIndex].item.icon;
					float quantity = playerItems [itemIndex].quantity;
					// show quantity if greater than 1
					if (quantity <= 0 || env.buildMode) {
						quantityText.enabled = false;
						quantityShadow.enabled = false;
					} else {
						string quantityStr = String.Format ("{0:0.##}", quantity);
						quantityText.text = quantityStr;
						quantityShadow.text = quantityStr;
						quantityText.enabled = true;
						quantityShadow.enabled = true;
					}
					// Mark selected item
					inventoryItemsImages [k].transform.Find ("SelectedBorder").gameObject.SetActive (k + itemsPerPage * inventoryCurrentPage == selectedItemIndex);
				} else {
					inventoryItemsImages [k].texture = Texture2D.whiteTexture;
					inventoryItemsImages [k].color = new Color (0, 0, 0, 0.25f);
					quantityText.enabled = false;
					quantityShadow.enabled = false;
					// Hide selected border
					inventoryItemsImages [k].transform.Find ("SelectedBorder").gameObject.SetActive (false);
				}
			}

			if (inventoryTitle != null) {
				if (playerItemsCount == 0) {
					inventoryTitle.SetActive (true);
					inventoryTitleText.text = "Empty.";
				} else if (playerItemsCount > itemsPerPage) {
					inventoryTitle.SetActive (true);
					int totalPages = (playerItemsCount - 1) / itemsPerPage + 1;
					if (totalPages < 0)
						totalPages = 1;
					inventoryTitleText.text = "Page " + (inventoryCurrentPage + 1) + "/" + totalPages;
				} else {
					inventoryTitle.SetActive (false);
				}
			}

		}


		void SelectItemFromVisibleInventorySlot (int itemIndex) {
			int slotIndex = itemIndex + columnToShow * _inventoryRows;
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			int selectedItemIndex = inventoryCurrentPage * itemsPerPage + slotIndex;
			VoxelPlayPlayer.instance.selectedItemIndex = selectedItemIndex;
		}


		/// <summary>
		/// Updates selected item representation on screen
		/// </summary>
		/// <param name="item">Item.</param>
		public void ShowSelectedItem (InventoryItem inventoryItem) {
			if (selectedItemPlaceholder == null || !env.enableInventory)
				return;
			ItemDefinition item = inventoryItem.item;
			selectedItem.texture = item.icon;
			string txt = item.title;
			if (string.IsNullOrEmpty (txt) && item.voxelType != null) {
				txt = item.voxelType.name;
			}
			selectedItemName.text = txt;
			selectedItemNameShadow.text = txt;
			selectedItemPlaceholder.SetActive (true);
			string quantity = inventoryItem.quantity.ToString ();
			bool quantityVisible = !VoxelPlayEnvironment.instance.buildMode;
			selectedItemQuantityShadow.enabled = quantityVisible;
			selectedItemQuantityShadow.text = quantity;
			selectedItemQuantity.enabled = quantityVisible;
			selectedItemQuantity.text = quantity;
			RefreshInventoryContents ();
			ToggleSelectedItemName ();
		}

		void ToggleSelectedItemName () {
			bool showItemName = inventoryPlaceholder.activeSelf && env.buildMode;
			selectedItemName.enabled = showItemName;
			selectedItemNameShadow.enabled = showItemName;
		}

		/// <summary>
		/// Hides selected item graphic
		/// </summary>
		public void HideSelectedItem () {
			if (selectedItemPlaceholder == null)
				return;
			selectedItemPlaceholder.SetActive (false);
			RefreshInventoryContents ();
		}

		#endregion


		#region Initialization Panel

		public void ToggleInitializationPanel (bool visible, float progress = 0) {
			if (!Application.isPlaying)
				return;

			if (initProgress == null) {
				CheckReferences ();
			}
			if (progress > 1)
				progress = 1f;
			initProgress.localScale = new Vector3 (progress, 1, 1);
			initPanel.SetActive (visible);
		}

		#endregion

		#region Debug Window

		public void ToggleDebugWindow (bool visible) {
			debug.SetActive (visible);
		}

		void UpdateDebugInfo () {

			sbDebug.Length = 0;
			VoxelChunk currentChunk = env.GetCurrentChunk ();
			if (currentChunk != null) {

				sbDebug.Append ("Current Chunk X=");
				AppendValueDebug (currentChunk.position.x);

				sbDebug.Append (", Y=");
				AppendValueDebug (currentChunk.position.y);

				sbDebug.Append (", Z=");
				AppendValueDebug (currentChunk.position.z);
			}
			VoxelChunk hitChunk = env.lastHitInfo.chunk;
			if (hitChunk != null) {
				int voxelIndex = env.lastHitInfo.voxelIndex;

				sbDebug.AppendLine ();

				sbDebug.Append ("Last Chunk Hit: X=");
				AppendValueDebug (hitChunk.position.x);

				sbDebug.Append (", Y=");
				AppendValueDebug (hitChunk.position.y);

				sbDebug.Append (", Z=");
				AppendValueDebug (hitChunk.position.z);
				int px, py, pz;
				env.GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);

				sbDebug.AppendLine ();

				sbDebug.Append ("Last Voxel Hit: X=");
				AppendValueDebug (px);

				sbDebug.Append (", Y=");
				AppendValueDebug (py);

				sbDebug.Append (", Z=");
				AppendValueDebug (pz);

				sbDebug.Append (", Index=");
				AppendValueDebug (env.lastHitInfo.voxelIndex);

				sbDebug.Append (", AboveTerrain=");
				AppendValueDebug (hitChunk.isAboveSurface);

				if (env.lastHitInfo.voxel.typeIndex != 0) {
					sbDebug.Append (", Type=");
					AppendValueDebug (env.lastHitInfo.voxel.type.name);
				}
			}
			sbDebug.AppendLine ();
			sbDebug.Append ("Mesh Jobs: Last=");
			int v0, v1, v2;
			env.GetMeshJobsStatus (out v0, out v1, out v2);
			AppendValueDebug (v0);
			sbDebug.Append (", CPU Generating=");
			AppendValueDebug (v1);
			sbDebug.Append (", GPU Upload=");
			AppendValueDebug (v2);

			debugText.text = sbDebug.ToString ();
		}

		void AppendValueDebug (object o) {
			sbDebug.Append ("<color=yellow>");
			sbDebug.Append (o);
			sbDebug.Append ("</color>");
		}

		#endregion

		#region FPS

		void ToggleFPS () {
			fpsShadow.gameObject.SetActive (!fpsShadow.gameObject.activeSelf);
		}

		void UpdateFPSCounter () {
			fpsTimeleft -= Time.deltaTime;
			fpsAccum += Time.timeScale / Time.deltaTime;
			++fpsFrames;
			if (fpsTimeleft <= 0.0) {
				int fps = (int)(fpsAccum / fpsFrames);
				fpsText.text = fps.ToString ();
				fpsShadow.text = fpsText.text;

				if (fps < 30)
					fpsText.color = Color.yellow;
				else if (fps < 10)
					fpsText.color = Color.red;
				else
					fpsText.color = Color.green;
				fpsTimeleft = fpsUpdateInterval;
				fpsAccum = 0.0f;
				fpsFrames = 0;
			}
		}


		#endregion

	
	}



}
