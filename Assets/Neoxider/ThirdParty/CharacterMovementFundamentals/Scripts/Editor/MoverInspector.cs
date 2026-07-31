using UnityEngine;
using UnityEditor;

namespace CMF
{
	//This editor script displays some additional information in the mover inspector, like a preview of the current raycast array;
	//NEOXIDER PATCH: it also shows the collider Mover generates from the fields above, read-only, so nobody spends time
	//typing values into the CapsuleCollider component only to have 'RecalculateColliderDimensions' overwrite them;
	[CustomEditor(typeof(Mover))]
	public class MoverInspector : Editor {

		private Mover mover;

		//NEOXIDER PATCH: handles for the read-only preview of the generated collider;
		private SerializedProperty stepHeightRatioProperty;
		private SerializedProperty colliderHeightProperty;
		private SerializedProperty colliderThicknessProperty;
		private SerializedProperty colliderOffsetProperty;

		//'colliderOffset' is normalised (multiplied by 'colliderHeight'), so Y = 0.5 raises the collider by half the
		//body height - which is exactly what puts the transform origin at the character's feet. Every CMF prefab uses it;
		private const float FeetAtOriginOffsetY = 0.5f;

		//Below a millimetre the origin is at the feet for every practical purpose;
		private const float OriginToleranceMetres = 0.001f;

		//NEOXIDER PATCH: whether the warning and its fix button belong to this layout cycle. Latched on the
		//Layout event and only there: 'Collider Offset' is editable in the default inspector drawn just above,
		//so reading it live would let the control count change midway through a single event pass and trip
		//GUILayout ("Getting control N's position in a group with only M controls") while the user drags Y
		//across 0.5. Latching keeps the Layout, Repaint and input passes drawing the same controls;
		private bool showOriginWarning;

		void Reset()
		{
			Setup();
		}

		void OnEnable()
		{
			Setup();
		}

		void Setup()
		{
			//Get reference to mover component;
			mover = (Mover)target;

			stepHeightRatioProperty = serializedObject.FindProperty("stepHeightRatio");
			colliderHeightProperty = serializedObject.FindProperty("colliderHeight");
			colliderThicknessProperty = serializedObject.FindProperty("colliderThickness");
			colliderOffsetProperty = serializedObject.FindProperty("colliderOffset");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawGeneratedColliderInfo();
			DrawRaycastArrayPreview();
		}

		//Show the collider dimensions Mover writes, read-only, plus where ground detection parks the transform origin;
		void DrawGeneratedColliderInfo()
		{
			if(mover == null || colliderHeightProperty == null || targets.Length > 1)
				return;

			float _stepHeightRatio = stepHeightRatioProperty.floatValue;
			float _colliderHeight = colliderHeightProperty.floatValue;
			float _colliderThickness = colliderThicknessProperty.floatValue;
			Vector3 _colliderOffset = colliderOffsetProperty.vector3Value;

			Collider _attachedCollider = mover.GetComponent<Collider>();

			GUILayout.Space(8);
			EditorGUILayout.LabelField("Generated Collider (read-only) :", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox(
				"These numbers are written by Mover into the " +
				(_attachedCollider != null ? _attachedCollider.GetType().Name : "collider") +
				" on Awake and on every inspector change. Editing that component directly does nothing - " +
				"change 'Collider Height' / 'Collider Thickness' / 'Collider Offset' above instead.",
				MessageType.Info);

			//Same formula 'RecalculateColliderDimensions' uses for all three collider types;
			Vector3 _center = _colliderOffset * _colliderHeight
				+ new Vector3(0f, _stepHeightRatio * _colliderHeight / 2f, 0f);

			using(new EditorGUI.DisabledScope(true))
			{
				if(_attachedCollider is CapsuleCollider)
				{
					float _height = _colliderHeight * (1f - _stepHeightRatio);
					float _radius = Mathf.Min(_colliderThickness / 2f, _height / 2f);

					EditorGUILayout.FloatField("Height", _height);
					EditorGUILayout.FloatField("Radius", _radius);
					EditorGUILayout.Vector3Field("Center", _center);
				}
				else if(_attachedCollider is SphereCollider)
				{
					EditorGUILayout.FloatField("Radius", _colliderHeight / 2f * (1f - _stepHeightRatio));
					EditorGUILayout.Vector3Field("Center", _center);
				}
				else if(_attachedCollider is BoxCollider)
				{
					EditorGUILayout.Vector3Field("Size", new Vector3(
						_colliderThickness,
						_colliderHeight * (1f - _stepHeightRatio),
						_colliderThickness));
					EditorGUILayout.Vector3Field("Center", _center);
				}
				else
				{
					EditorGUILayout.LabelField("No collider attached - Mover adds a CapsuleCollider itself.");
				}
			}

			//Standing on flat ground the collider always occupies [stepHeight .. colliderHeight] above the floor,
			//whatever the offset is; the offset only decides where the transform origin ends up inside that body;
			float _stepGap = _stepHeightRatio * _colliderHeight;
			float _originAboveGround = _colliderHeight * (FeetAtOriginOffsetY - _colliderOffset.y);

			EditorGUILayout.LabelField("Body above floor",
				string.Format("{0:0.###} .. {1:0.###} m (lower gap = step height)", _stepGap, _colliderHeight));
			EditorGUILayout.LabelField("Transform origin at rest",
				string.Format("{0:0.###} m above the floor", _originAboveGround));

			if(Event.current.type == EventType.Layout)
				showOriginWarning = Mathf.Abs(_originAboveGround) > OriginToleranceMetres;

			if(!showOriginWarning)
				return;

			EditorGUILayout.HelpBox(
				string.Format(
					"'Collider Offset' Y is {0:0.###}, so ground detection parks the transform origin {1:0.###} m above the floor. " +
					"Children placed as if the origin were the feet - character model, camera pivot - hang that far in the air. " +
					"Y = 0.5 puts the origin exactly at the feet and is what every CMF prefab ships with.",
					_colliderOffset.y, _originAboveGround),
				MessageType.Warning);

			if(!GUILayout.Button("Set Collider Offset Y to 0.5 (origin at feet)"))
				return;

			Collider _colliderToRecord = mover.GetComponent<Collider>();

			if(_colliderToRecord != null)
				Undo.RecordObject(_colliderToRecord, "Set Mover Collider Offset");

			_colliderOffset.y = FeetAtOriginOffsetY;
			colliderOffsetProperty.vector3Value = _colliderOffset;
			serializedObject.ApplyModifiedProperties();

			//'OnValidate' only recalculates for objects that are active in a hierarchy, so a prefab asset selected in
			//the project window would keep the stale collider - write it here as well;
			mover.RecalculateColliderDimensions();

			if(_colliderToRecord != null)
				EditorUtility.SetDirty(_colliderToRecord);
		}

		//Draw preview of raycast array in inspector;
		void DrawRaycastArrayPreview()
		{
			if(mover.sensorType == Sensor.CastType.RaycastArray)
			{
				Rect _space;
				GUILayout.Space(5);

				_space = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(100));

				Rect background = new Rect(_space.x + (_space.width - _space.height)/2f, _space.y, _space.height, _space.height);
				EditorGUI.DrawRect(background, Color.grey);

				float point_size = 3f;

				Vector3[] _previewPositions = mover.raycastArrayPreviewPositions;

				Vector2 center = new Vector2(background.x + background.width/2f, background.y + background.height/2f);

				if(_previewPositions != null && _previewPositions.Length != 0)
				{
					for(int i = 0; i < _previewPositions.Length; i++)
					{
						Vector2 position = center + new Vector2(_previewPositions[i].x, _previewPositions[i].z) * background.width/2f * 0.9f;

						EditorGUI.DrawRect(new Rect(position.x - point_size/2f, position.y - point_size/2f, point_size, point_size), Color.white);
					}
				}

				if(_previewPositions != null && _previewPositions.Length != 0)
					GUILayout.Label("Number of rays = " + _previewPositions.Length, EditorStyles.centeredGreyMiniLabel );
			}
		}


	}
}
