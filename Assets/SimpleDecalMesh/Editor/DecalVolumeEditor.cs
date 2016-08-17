using UnityEngine;
using UnityEditor;

namespace SimpleDecalMesh
{
    [CanEditMultipleObjects, CustomEditor(typeof(DecalVolume))]
    public class DecalVolumeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var decal = (DecalVolume)target;

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                decal.Clear();
                Undo.RecordObject(target, "Decal Volume changes");
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Update"))
            {
                decal.Create();
            }

            if (GUILayout.Button("Update All"))
            {
                DecalVolume[] decals = FindObjectsOfType<DecalVolume>();
                for (int i = 0; i < decals.Length; i++)
                {
                    decals[i].Create();
                }
            }
        }

        private void OnSceneGUI()
        {
            var decal = (DecalVolume)target;
            var volume = TransformVolume.EditorVolumeControl(decal, 0.075f, DecalVolume.EditorColor);

            if (volume != decal.Volume || decal.transform.hasChanged)
            {
                decal.Clear();
                Undo.RecordObject(target, "Decal Volume changes");

                decal.Volume = volume;
                decal.transform.hasChanged = false;

                EditorUtility.SetDirty(target);
            }
        }
        
        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.Active)]
        private static void DrawGizmoVolume(DecalVolume decal, GizmoType gizmoType)
        {
            if (gizmoType != (GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.Active)) return;

            //
            //  Uncomment this for debug affected bounds of volume.
            //
            //Gizmos.color = Color.red;
            //Bounds bounds = decal.GetBounds();
            //Gizmos.DrawWireCube(bounds.center, bounds.size);

            Vector3 pos = decal.transform.position;
            Gizmos.matrix = Matrix4x4.TRS(pos, decal.transform.rotation, Vector3.one);

            var color = DecalVolume.EditorColor;
            color.a = 0.25f;
            Gizmos.color = color;

            Gizmos.DrawCube(decal.Origin, decal.Size);
        }
        
        [MenuItem("GameObject/3D Object/Decal")]
        private static void CreateDecalVolume(MenuCommand menuCommand)
        {
            var go = new GameObject("Decal");
            go.AddComponent<MeshFilter>();

            var re = go.AddComponent<MeshRenderer>();
            re.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            re.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<DecalVolume>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Decal");
            Selection.activeGameObject = go;
        }
    }
}
