using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace VoxelPlay {

	[RequireComponent (typeof(CharacterController))]
	[RequireComponent (typeof(AudioSource))]
	[ExecuteInEditMode]
	public partial class VoxelPlayFirstPersonController : VoxelPlayCharacterControllerBase {

		[Header ("Movement")]
		public float walkSpeed = 5f;
		public float runSpeed = 10f;
		public float flySpeed = 20f;
		public float swimSpeed = 3.7f;
		public  float jumpSpeed = 10f;
		public  float stickToGroundForce = 10f;
		public  float gravityMultiplier = 2f;
		public  MouseLook mouseLook;
		public  bool useFovKick = true;
		[SerializeField] private FOVKick m_FovKick = new FOVKick ();
		public bool useHeadBob = true;
		[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob ();
		[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob ();

		[Header ("Orbit")]
		public bool orbitMode;
		public Vector3 lookAt;
		public float minDistance = 1f;
		public float maxDistance = 100f;


		// internal fields
		Camera m_Camera;
		bool m_Jump;
		Vector3 m_Input;
		Vector3 m_MoveDir = Misc.vector3zero;
		CharacterController m_CharacterController;
		CollisionFlags m_CollisionFlags;
		bool m_PreviouslyGrounded;
		Vector3 m_OriginalCameraPosition;
		bool m_Jumping;
		float lastHitButtonPressed;
		GameObject underwaterPanel;
		Material underWaterMat;
		Transform crouch;

		int lastNearClipPosX, lastNearClipPosY, lastNearClipPosZ;
		Vector3 curPos;
		float waterLevelTop;

		const float switchDuration = 2f;
		bool switching;
		float switchingStartTime;
		float switchingLapsed;
		VoxelPlayInputController input;

		static VoxelPlayFirstPersonController _firstPersonController;

		public static VoxelPlayFirstPersonController instance {
			get {
				if (_firstPersonController == null) {
					_firstPersonController = FindObjectOfType<VoxelPlayFirstPersonController> ();
				}
				return _firstPersonController;
			}
		}

		void OnEnable () {
			m_CharacterController = GetComponent<CharacterController> ();
			m_CharacterController.stepOffset = 0.4f;
			env = VoxelPlayEnvironment.instance;
			if (env == null) {
				Debug.LogError ("Voxel Play Environment must be added first.");
			} else {
				env.characterController = this;
			}
			crouch = transform.Find ("Crouch").transform;
		}

		void Start () {
			base.Init ();
			m_Camera = GetComponentInChildren<Camera> ();
			if (env != null)
				env.cameraMain = m_Camera;
			m_Camera.nearClipPlane = 0.1f; // good value for interacting with water
			m_OriginalCameraPosition = m_Camera.transform.localPosition;
			m_FovKick.Setup (m_Camera);
			m_HeadBob.Setup (m_Camera, footStepInterval);
			m_Jumping = false;

			if (env == null || !env.applicationIsPlaying)
				return;

			underwaterPanel = Instantiate<GameObject> (Resources.Load<GameObject> ("VoxelPlay/Prefabs/UnderwaterPanel"), m_Camera.transform);
			underwaterPanel.name = "UnderwaterPanel";
			Renderer underwaterRenderer = underwaterPanel.GetComponent<Renderer> ();
			underWaterMat = underwaterRenderer.sharedMaterial;
			underWaterMat = Instantiate<Material> (underWaterMat);
			underwaterRenderer.sharedMaterial = underWaterMat;

			underwaterPanel.transform.localPosition = new Vector3 (0, 0, m_Camera.nearClipPlane + 0.001f);
			underwaterPanel.SetActive (false);

			ToggleCharacterController (false);

			// Position character on ground
			if (!env.saveFileIsLoaded) {
				if (startOnFlat && env.world != null) {
					float minAltitude = env.world.terrainGenerator.maxHeight;
					Vector3 flatPos = transform.position;
					Vector3 randomPos;
					for (int k = 0; k < startOnFlatIterations; k++) {
						randomPos = Random.insideUnitSphere * 1000;
						float alt = env.GetTerrainHeight (randomPos);
						if (alt < minAltitude && alt >= env.waterLevel + 1) {
							minAltitude = alt;
							randomPos.y = alt + m_CharacterController.height + 1;
							flatPos = randomPos;
						}
					}
					transform.position = flatPos;
				}
			}

			input = env.input;

			InitCrosshair ();

			SetOrbitMode (orbitMode);
			mouseLook.Init (transform, m_Camera.transform, input);

			if (!env.initialized) {
				env.OnInitialized += () => WaitForCurrentChunk ();
			} else {
				WaitForCurrentChunk ();
			}
		}


		/// <summary>
		/// Disables character controller until chunk is ready
		/// </summary>
		public void WaitForCurrentChunk () {
			ToggleCharacterController (false);
			StartCoroutine (WaitForCurrentChunkCoroutine ());
		}

		/// <summary>
		/// Enables/disables character controller
		/// </summary>
		/// <param name="state">If set to <c>true</c> state.</param>
		public void ToggleCharacterController (bool state) {
			m_CharacterController.enabled = state;
			enabled = state;
		}

		/// <summary>
		/// Ensures player chunk is finished before allow player movement / interaction with colliders
		/// </summary>
		IEnumerator WaitForCurrentChunkCoroutine () {
			// Wait until current player chunk is rendered
			WaitForSeconds w = new WaitForSeconds (0.2f);
			for (int k = 0; k < 20; k++) {
				VoxelChunk chunk = env.GetCurrentChunk ();
				if (chunk != null && chunk.isRendered) {
					break;
				}
				yield return w;
			}
			Unstuck (true);
			ToggleCharacterController (true);
		}

		void Update () {
			if (env == null || !env.applicationIsPlaying || !env.initialized || input == null)
				return;

			RotateView ();

			if (orbitMode)
				isFlying = true;

			// the jump state needs to read here to make sure it is not missed
			if (!m_Jump && !isFlying) {
				m_Jump = input.GetButtonDown (INPUT_BUTTON_NAMES.Jump);
			}

			curPos = transform.position;

			CheckFootfalls ();

			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded) {
				StartCoroutine (m_JumpBob.DoBobCycle ());
				PlayLandingSound ();
				m_MoveDir.y = 0f;
				m_Jumping = false;
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded) {
				m_MoveDir.y = 0f;
			}

			m_PreviouslyGrounded = m_CharacterController.isGrounded;

			// Process click events
			if (input.focused && input.enabled) {
				bool leftAltPressed = input.GetButton (INPUT_BUTTON_NAMES.LeftAlt);
				bool leftShiftPressed = input.GetButton (INPUT_BUTTON_NAMES.LeftShift);
				bool leftControlPressed = input.GetButton (INPUT_BUTTON_NAMES.LeftControl);
				bool fire1Pressed = input.GetButton (INPUT_BUTTON_NAMES.Button1);
				bool fire2Clicked = input.GetButtonDown (INPUT_BUTTON_NAMES.Button2);
				if (!leftShiftPressed && !leftAltPressed && !leftControlPressed) {
					if (Time.time - lastHitButtonPressed > player.hitDelay) {
						if (fire1Pressed) {
							DoHit (player.hitDamage);
						}
					}
					if (fire2Clicked) {
						DoHit (0);
					}
				}

				if (crosshairOnBlock && input.GetButtonDown (INPUT_BUTTON_NAMES.MiddleButton)) {
					player.SetSelectedItem (crosshairHitInfo.voxel.type);
				}

				if (input.GetButtonDown (INPUT_BUTTON_NAMES.Build)) {
					env.SetBuildMode (!env.buildMode);
					if (env.buildMode) {
						#if UNITY_EDITOR
						env.ShowMessage ("<color=green>Entered <color=yellow>Build Mode</color>. Press <color=white>B</color> to cancel, <color=white>V</color> to enter the Constructor.</color>");
						#else
						env.ShowMessage ("<color=green>Entered <color=yellow>Build Mode</color>. Press <color=white>B</color> to cancel.</color>");
						#endif
					} else {
						env.ShowMessage ("<color=green>Back to <color=yellow>Normal Mode</color>.</color>");
					}
				}

				if (fire2Clicked && !leftAltPressed && !leftShiftPressed) {
					DoBuild ();
				}

				// Toggles Flight mode
				if (input.GetButtonDown (INPUT_BUTTON_NAMES.Fly)) {
					isFlying = !isFlying;
					if (isFlying) {
						env.ShowMessage ("<color=green>Flying <color=yellow>ON</color></color>");
					} else {
						env.ShowMessage ("<color=green>Flying <color=yellow>OFF</color></color>");
					}
				}

				if (isGrounded && !isCrouched && input.GetButtonDown (INPUT_BUTTON_NAMES.LeftControl)) {
					isCrouched = true;
				} else if (isGrounded && isCrouched && input.GetButtonUp (INPUT_BUTTON_NAMES.LeftControl)) {
					isCrouched = false;
				} else if (isGrounded && input.GetButtonDown (INPUT_BUTTON_NAMES.Crouch)) {
					isCrouched = !isCrouched;
					if (isCrouched) {
						env.ShowMessage ("<color=green>Crouching <color=yellow>ON</color></color>");
					} else {
						env.ShowMessage ("<color=green>Crouching <color=yellow>OFF</color></color>");
					}
				} else if (input.GetButtonDown (INPUT_BUTTON_NAMES.Light)) {
					ToggleCharacterLight ();
				} else if (input.GetButtonDown (INPUT_BUTTON_NAMES.ThrowItem)) {
					ThrowCurrentItem ();
				}
			}

			// Check water
			CheckWaterStatus ();

			// Check crouch status
			if (!isInWater) {
				if (isCrouched && crouch.localPosition.y == 0) {
					crouch.transform.localPosition = new Vector3 (0, -1f, 0);
					m_CharacterController.stepOffset = 0.4f;
				} else if (!isCrouched && crouch.localPosition.y != 0) {
					crouch.transform.localPosition = Misc.vector3zero;
					m_CharacterController.stepOffset = 1f;
				}
			}

			#if UNITY_EDITOR
			UpdateConstructor ();
			#endif

		}

		public void SetOrbitMode (bool enableOrbitMode) {
			if (orbitMode != enableOrbitMode) {
				orbitMode = enableOrbitMode;
				switching = true;
				switchingStartTime = Time.time;
			}
			freeMode = orbitMode;
		}


		void CheckWaterStatus () {

			Vector3 nearClipPos = m_Camera.transform.position + m_Camera.transform.forward * (m_Camera.nearClipPlane + 0.001f);
			if (nearClipPos.x == lastNearClipPosX && nearClipPos.y == lastNearClipPosY && nearClipPos.z == lastNearClipPosZ)
				return;

			lastNearClipPosX = (int)nearClipPos.x;
			lastNearClipPosY = (int)nearClipPos.y;
			lastNearClipPosZ = (int)nearClipPos.z;

			bool wasInWater = isInWater;

			isInWater = false;
			isSwimming = false;
			isUnderwater = false;

			// Check water on character controller position
			VoxelChunk chunk;
			int voxelIndex;
			Voxel voxelCh;
			if (env.GetVoxelIndex (curPos, out chunk, out voxelIndex, false)) {
				voxelCh = chunk.voxels [voxelIndex];
			} else {
				voxelCh = Voxel.Empty;
			}
			VoxelDefinition voxelChType = env.voxelDefinitions [voxelCh.typeIndex];
			if (voxelCh.hasContent == 1) {
				CheckEnterTrigger (chunk, voxelIndex);
				CheckDamage (voxelChType);
			}

			// Safety check; if voxel at character position is solid, move character on top of terrain
			if (voxelCh.isSolid) {
				Unstuck (false);
			} else {

				// Check if water surrounds camera
				Voxel voxelCamera = env.GetVoxel (nearClipPos);
				VoxelDefinition voxelCameraType = env.voxelDefinitions [voxelCamera.typeIndex];
				if (voxelCamera.hasContent == 1) {
					CheckEnterTrigger (chunk, voxelIndex);
					CheckDamage (voxelCameraType);
				}

				if (voxelCamera.GetWaterLevel () > 7) {
					// More water on top?
					Vector3 pos1Up = nearClipPos;
					pos1Up.y += 1f;
					Voxel voxel1Up = env.GetVoxel (pos1Up);
					if (voxel1Up.GetWaterLevel () > 0) {
						isUnderwater = true;
						waterLevelTop = nearClipPos.y + 1f;
					} else {
						waterLevelTop = FastMath.FloorToInt (nearClipPos.y) + 0.9f;
						isUnderwater = nearClipPos.y < waterLevelTop;
						isSwimming = !isUnderwater;
					}
					underWaterMat.color = voxelCameraType.diveColor;
				} else if (voxelCh.GetWaterLevel () > 7) { // type == env.world.waterVoxel) {
					isSwimming = true;
					waterLevelTop = FastMath.FloorToInt (curPos.y) + 0.9f;
					underWaterMat.color = voxelChType.diveColor;

				}
				underWaterMat.SetFloat ("_WaterLevel", waterLevelTop);
			}

			isInWater = isSwimming || isUnderwater;
			if (!wasInWater && isInWater) {
				PlayWaterSplashSound ();
				crouch.localPosition = Misc.vector3down * 0.6f; // crouch
			} else if (wasInWater && !isInWater) {
				crouch.localPosition = Misc.vector3zero;
			}

			// Show/hide underwater panel
			if (isInWater && !underwaterPanel.activeSelf) {
				underwaterPanel.SetActive (true);
			} else if (!isInWater && underwaterPanel.activeSelf) {
				underwaterPanel.SetActive (false);
			}
		}



		void DoHit (int damage) {
			lastHitButtonPressed = Time.time;
			Ray ray;
			if (freeMode) {
				ray = m_Camera.ScreenPointToRay (input.screenPos);
			} else {
				ray = m_Camera.ViewportPointToRay (Misc.vector2half);
			}
			env.RayHit (ray, damage, player.hitRange, player.hitDamageRadius);
		}


		void DoBuild () {
			if (player.selectedItemIndex < 0 || player.selectedItemIndex >= player.items.Count)
				return;

			InventoryItem inventoryItem = player.GetSelectedItem ();
			ItemDefinition currentItem = inventoryItem.item;
			switch (currentItem.category) {
			case ITEM_CATEGORY.Voxel:
				
				// Basic placement rules
				bool canPlace = crosshairOnBlock;
				Voxel existingVoxel = crosshairHitInfo.voxel;
				VoxelDefinition existingVoxelType = existingVoxel.type;
				Vector3 placePos;
				if (currentItem.voxelType.renderType == RenderType.Water && !canPlace) {
					canPlace = true; // water can be poured anywhere
					placePos = m_Camera.transform.position + m_Camera.transform.forward * 3f;
				} else {
					placePos = crosshairHitInfo.voxelCenter + crosshairHitInfo.normal;
					if (canPlace && crosshairHitInfo.normal.y == 1) {
						// Make sure there's a valid voxel under position (ie. do not build a voxel on top of grass)
						canPlace = (existingVoxelType != null && existingVoxelType.renderType != RenderType.CutoutCross && (existingVoxelType.renderType != RenderType.Water || currentItem.voxelType.renderType == RenderType.Water));
					}
				}
				VoxelDefinition placeVoxelType = currentItem.voxelType;

				// Check voxel promotion
				bool isPromoting = false;
				if (canPlace) {
					if (existingVoxelType == currentItem.voxelType) {
						if (existingVoxelType.promotesTo != null) {
							// Promote existing voxel
							env.VoxelDestroy (crosshairHitInfo.voxelCenter);
							placePos = crosshairHitInfo.voxelCenter;
							placeVoxelType = existingVoxelType.promotesTo;
							isPromoting = true;
						} else if (crosshairHitInfo.normal.y > 0 && existingVoxelType.biomeDirtCounterpart != null) {
							env.VoxelPlace (crosshairHitInfo.voxelCenter, existingVoxelType.biomeDirtCounterpart);
						}
					}
				}

				// Compute rotation
				int textureRotation = 0;
				if (placeVoxelType.placeFacingPlayer && placeVoxelType.renderType.supportsTextureRotation ()) {
					// Orient voxel to player
					Vector3 dir = m_Camera.transform.forward;
					if (Mathf.Abs (dir.x) > Mathf.Abs (dir.z)) {
						if (dir.x > 0) {
							textureRotation = 1;
						} else {
							textureRotation = 3;
						}
					} else if (dir.z < 0) {
						textureRotation = 2;
					}
				}

				// Final check, does it overlap existing geometry?
				if (canPlace && !isPromoting) {
					Quaternion rotationQ = Quaternion.Euler (0, Voxel.GetTextureRotationDegrees (textureRotation), 0);
					canPlace = !env.VoxelOverlaps (placePos, placeVoxelType, rotationQ, 1 << env.layerVoxels);
					if (!canPlace) {
						PlayCancelSound ();
					}
				}
#if UNITY_EDITOR
                else if (env.constructorMode) {
					placePos = voxelHighlightBuilder.transform.position;
					placeVoxelType = currentItem.voxelType;
					canPlace = true;
				}
#endif
				// Finally place the voxel
				if (canPlace) {
					// Consume item first
					if (!env.buildMode) {
						player.ConsumeItem ();
					}
					// Place it
					float amount = inventoryItem.quantity < 1f ? inventoryItem.quantity : 1f;
					env.VoxelPlace (placePos, placeVoxelType, true, placeVoxelType.tintColor, amount, textureRotation);

					// Moves back character controller if voxel is put just on its position
					const float minDist = 0.5f;
					float distSqr = Vector3.SqrMagnitude (m_Camera.transform.position - placePos);
					if (distSqr < minDist * minDist) {
						m_CharacterController.transform.position += crosshairHitInfo.normal;
					}

				}
				break;
			case ITEM_CATEGORY.Torch:
				if (crosshairOnBlock) {
					GameObject torchAttached = env.TorchAttach (crosshairHitInfo);
					if (!env.buildMode && torchAttached != null) {
						player.ConsumeItem ();
					}
				}
				break;
			}
		}

		private void FixedUpdate () {
			float speed;
			GetInput (out speed);

			Vector3 pos = transform.position;
			if (!m_Jumping && (isFlying || isInWater)) {
				m_MoveDir = m_Camera.transform.forward * m_Input.y + m_Camera.transform.right * m_Input.x + m_Camera.transform.up * m_Input.z;
				m_MoveDir *= speed;
				if (!isFlying) {
					if (m_MoveDir.y < 0) {
						m_MoveDir.y += 0.1f * Time.fixedDeltaTime;
					}
					if (m_Jump) {
						// Check if player is next to terrain
						if (env.CheckCollision (new Vector3 (pos.x + m_Camera.transform.forward.x, pos.y, pos.z + m_Camera.transform.forward.z))) {
							m_MoveDir.y = jumpSpeed * 0.5f;
							m_Jumping = true;
						}
						m_Jump = false;
					} else {
						m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime * 0.5f;
					}
					if (pos.y > waterLevelTop && m_MoveDir.y > 0) {
						m_MoveDir.y = 0; // do not exit water
					}
					ProgressSwimCycle (m_CharacterController.velocity, swimSpeed);
				}
			} else {
				// always move along the camera forward as it is the direction that it being aimed at
				Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

				// get a normal for the surface that is being touched to move along it
				RaycastHit hitInfo;
				Physics.SphereCast (pos, m_CharacterController.radius, Misc.vector3down, out hitInfo,
					m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
				desiredMove = Vector3.ProjectOnPlane (desiredMove, hitInfo.normal).normalized;

				m_MoveDir.x = desiredMove.x * speed;
				m_MoveDir.z = desiredMove.z * speed;
				if (m_CharacterController.isGrounded) {
					m_MoveDir.y = -stickToGroundForce;

					if (m_Jump) {
						m_MoveDir.y = jumpSpeed;
						PlayJumpSound ();
						m_Jump = false;
						m_Jumping = true;
					}
				} else {
					m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
				}

				UpdateCameraPosition (speed);
				ProgressStepCycle (m_CharacterController.velocity, speed);
			}


			Vector3 finalMove = m_MoveDir * Time.fixedDeltaTime;
			Vector3 newPos = pos + finalMove;
			bool canMove = !limitBoundsEnabled || limitBounds.Contains (newPos);
			if (m_PreviouslyGrounded && !isFlying && isCrouched) {
				// check if player is beyond the edge
				Ray ray = new Ray (newPos, Misc.vector3down);
				canMove = Physics.SphereCast (ray, 0.3f, 1f);
				// if player can't move, clamp movement along the edge and check again
				if (!canMove) {
					if (Mathf.Abs (m_MoveDir.z) > Mathf.Abs (m_MoveDir.x)) {
						m_MoveDir.x = 0;
					} else {
						m_MoveDir.z = 0;
					}
					finalMove = m_MoveDir * Time.fixedDeltaTime;
					newPos = pos + finalMove;
					ray.origin = newPos;
					canMove = Physics.SphereCast (ray, 0.3f, 1f);
				}
			}

			// if constructor is enabled, disable any movement if control key is pressed (reserved for special constructor actions)
			if (env.constructorMode && input.GetButton (INPUT_BUTTON_NAMES.LeftControl)) {
				canMove = false;
			}
			if (canMove) {
				// autoclimb
				Vector3 dir = new Vector3 (m_MoveDir.x, 0, m_MoveDir.z);
				Vector3 basePos = new Vector3 (pos.x, pos.y - m_CharacterController.height * 0.25f, pos.z);
				Ray ray = new Ray (basePos, dir);
				if (Physics.SphereCast (ray, 0.3f, 1f)) {
					m_CharacterController.stepOffset = 1.1f;
				} else {
					m_CharacterController.stepOffset = 0.2f;
				}
				m_CollisionFlags = m_CharacterController.Move (finalMove);
			}
			isGrounded = m_CharacterController.isGrounded;

			// Check limits
			if (orbitMode) {
				if (FastVector.ClampDistance (ref lookAt, ref pos, minDistance, maxDistance)) {
					m_CharacterController.transform.position = pos;
				}
			}

			mouseLook.UpdateCursorLock ();

			if (!isGrounded && !isFlying) {
				// Check current chunk
				VoxelChunk chunk = env.GetCurrentChunk ();
				if (chunk != null && !chunk.isRendered) {
					WaitForCurrentChunk ();
					return;
				}
			}


		}



		private void UpdateCameraPosition (float speed) {
			Vector3 newCameraPosition;
			if (!useHeadBob) {
				return;
			}
			if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded) {
				m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob (m_CharacterController.velocity.magnitude +
				(speed * (isMoving ? 1f : runstepLenghten)));
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset ();
			} else {
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset ();
			}

			m_Camera.transform.localPosition = newCameraPosition;
		}


		private void GetInput (out float speed) {
			float up = 0;
			bool waswalking = isMoving;
			if (input == null || !input.enabled) {
				speed = 0;
				return;
			}

			if (input.GetButton (INPUT_BUTTON_NAMES.Up)) {
				up = 1f;
			} else if (input.GetButton (INPUT_BUTTON_NAMES.Down)) {
				up = -1f;
			}

			bool leftShiftPressed = input.GetButton (INPUT_BUTTON_NAMES.LeftShift);
			isMoving = isGrounded && !isInWater && !isFlying && !leftShiftPressed;
			isRunning = false;

			// set the desired speed to be walking or running
			if (isFlying) {
				speed = leftShiftPressed ? flySpeed * 2 : flySpeed;
			} else if (isInWater) {
				speed = swimSpeed;
			} else if (isCrouched) {
				speed = walkSpeed * 0.25f;
			} else if (isMoving) {
				speed = walkSpeed;
			} else {
				speed = runSpeed;
				isRunning = true;
			}
			m_Input = new Vector3 (input.horizontalAxis, input.verticalAxis, up);

			// normalize input if it exceeds 1 in combined length:
			if (m_Input.sqrMagnitude > 1) {
				m_Input.Normalize ();
			}

			isPressingMoveKeys = m_Input.x != 0 || m_Input.y != 0;

			// handle speed change to give an fov kick
			// only if the player is going to a run, is running and the fovkick is to be used
			if (isMoving != waswalking && useFovKick && m_CharacterController.velocity.sqrMagnitude > 0) {
				StopAllCoroutines ();
				StartCoroutine (!isMoving ? m_FovKick.FOVKickUp () : m_FovKick.FOVKickDown ());
			}

		}


		private void RotateView () {
			if (switching) {
				switchingLapsed = (Time.time - switchingStartTime) / switchDuration;
				if (switchingLapsed > 1f) {
					switchingLapsed = 1f;
					switching = false;
				}
			} else {
				switchingLapsed = 1;
			}
			mouseLook.LookRotation (transform, m_Camera.transform, orbitMode, lookAt, switchingLapsed);
		}


		private void OnControllerColliderHit (ControllerColliderHit hit) {
			Rigidbody body = hit.collider.attachedRigidbody;
			//dont move the rigidbody if the character is on top of it
			if (m_CollisionFlags == CollisionFlags.Below) {
				return;
			}

			if (body == null || body.isKinematic) {
				return;
			}
			body.AddForceAtPosition (m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
		}

		/// <summary>
		/// Ensures player is above terrain
		/// </summary>
		public void Unstuck (bool toSurface = true) {
			if (env.CheckCollision (env.cameraMain.transform.position) || env.CheckCollision (transform.position)) {
				float minAltitude = Mathf.FloorToInt (transform.position.y) + 1.1f;
				if (toSurface) {
					minAltitude = Mathf.Max (env.GetTerrainHeight (transform.position), minAltitude);
				}
				transform.position = new Vector3 (transform.position.x, minAltitude + m_CharacterController.height * 0.5f, transform.position.z);
			}
		}

		/// <summary>
		/// Removes an unit fcrom current item in player inventory and throws it into the scene
		/// </summary>
		public void ThrowCurrentItem () {
			InventoryItem inventoryItem = player.ConsumeItem ();
			if (inventoryItem == InventoryItem.Null)
				return;

			if (inventoryItem.item.category != ITEM_CATEGORY.Voxel)
				return;

			env.VoxelThrow (m_Camera.transform.position, m_Camera.transform.forward, 15f, inventoryItem.item.voxelType, Misc.color32White);
		}

	}
}
