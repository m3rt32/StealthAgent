﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Subtegral.StealthAgent.Interactions;
using Subtegral.StealthAgent.GameCore;
using Pathfinding;
using UnityEditor.IMGUI.Controls;
namespace Subtegral.StealthAgent.EditorSystem
{
    public class LevelDesigner : EditorWindow
    {
        static Transform doorAnchored = null;
        AstarPath Path
        {
            get
            {
                return FindObjectOfType<AstarPath>();
            }
        }

        static bool editEnemyRadius;

        Vector3 angle = Vector3.zero;
        Vector3 radius = Vector3.one * 2f;

        string levelName = "";

        public Vector3 facingDirection
        {
            get
            {
                Vector3 result = Vector3.forward;
                result.y = 0f;
                return result.sqrMagnitude == 0f ? Vector3.forward : result.normalized;
            }
        }


        [MenuItem("Subtegral/Level Editor")]
        public static void OpenEditor()
        {
            LevelDesigner designer = GetWindow<LevelDesigner>("Level Editor");
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {

            if (EditorApplication.isPlaying)
                return;
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(5, 5, 100, 300), EditorStyles.helpBox);
            if (GUILayout.Button("New Block"))
            {
                GameObject shape = Resources.Load<GameObject>("Editor/ShapeBase");
                shape = Instantiate(shape);
                shape.transform.SetParent((GameObject.Find("ObstacleRoot") ?? new GameObject("ObstacleRoot")).transform, true);
                shape.name = "Obstacle";
                Selection.activeGameObject = shape;
                Tools.current = Tool.Move;
            }


            GUILayout.Label("Prefabs", EditorStyles.boldLabel);
            DisplayPrefabs("Camera");
            DisplayPrefabs("Door", (Transform g) => { doorAnchored = g; });
            DisplayPrefabs("EnemyBase");
            DisplayPrefabs("Laser");


            GUILayout.Label("Items", EditorStyles.boldLabel);
            DisplayMenuItem("HackTarget");
            DisplayMenuItem("Key");
            DisplayMenuItem("NoiseItem");
            DisplayMenuItem("TaserItem");

            GUI.color = Color.red;
            if (GUILayout.Button("DEL"))
            {
                if (Selection.activeGameObject != null)
                    DestroyImmediate(Selection.activeGameObject);
            }
            GUI.color = Color.white;

            GUILayout.EndArea();



            if (doorAnchored != null)
            {
                GUILayout.BeginArea(new Rect(Camera.current.pixelRect.width / 2f, 5, 150, 25), EditorStyles.helpBox);
                if (GUILayout.Button("Create Anchor Point"))
                {
                    GameObject gm = new GameObject("Door Anchor");
                    ((DoorData)doorAnchored.GetComponent<Door>().GetContainer()).AnchorPoint = gm.transform;
                    gm.transform.SetParent(doorAnchored, true);
                    doorAnchored = gm.transform;
                    Selection.activeGameObject = gm;
                }
                GUILayout.EndArea();
            }

            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.GetComponent<Enemy>())
                {
                    GUILayout.BeginArea(new Rect(Camera.current.pixelRect.width / 2f, 5, 250, 25), EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add WayPoint"))
                    {
                        Enemy selectedEnemy = Selection.activeGameObject.GetComponent<Enemy>();
                        ((EnemyData)selectedEnemy.GetContainer()).Waypoints.Add(new Waypoint()
                        {
                            Angles = new List<WaypointAngle>()
                            {
                                new WaypointAngle()
                                {
                                    Angle = 0f,
                                    TimeToWait =1f,
                                    LookAtRatio = 1f
                                },
                                new WaypointAngle()
                                {
                                    Angle = 60f,
                                    TimeToWait =1f,
                                    LookAtRatio = 1f
                                }
                            }
                        });
                        EditorUtility.SetDirty(selectedEnemy);
                    }
                    if (editEnemyRadius)
                        GUI.color = Color.gray;
                    if (GUILayout.Button("Set Search Radius"))
                    {
                        editEnemyRadius ^= true;
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                    GUILayout.EndArea();
                }
            }


            Handles.EndGUI();



            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.GetComponent<Enemy>())
                {

                    Enemy selectedEnemy = Selection.activeGameObject.GetComponent<Enemy>();
                    if (editEnemyRadius)
                    {
                        ((EnemyData)selectedEnemy.GetContainer()).RandomPointRadius = Handles.RadiusHandle(Quaternion.identity, Path.data.gridGraph.center, ((EnemyData)selectedEnemy.GetContainer()).RandomPointRadius);
                    }
                    else
                    {
                        List<Waypoint> waypoints = ((EnemyData)selectedEnemy.GetContainer()).Waypoints;
                        List<Vector3> positions = new List<Vector3>();
                        for (int i = 0; i < waypoints.Count; i++)
                        {
                            Handles.color = Color.red;
                            if (Handles.Button(waypoints[i].Position + new Vector2(1f, 1f), Quaternion.identity, .3f, .3f, Handles.SphereHandleCap))
                            {
                                waypoints.Remove(waypoints[i]);
                                break;
                            }
                            Handles.color = Color.white;
                            waypoints[i].Position = Handles.PositionHandle(waypoints[i].Position, Quaternion.identity);

                            positions.Add(waypoints[i].Position);
                            if (waypoints[i].Angles.Count > 1)
                            {
                                JointAngularLimitHandle(waypoints[i].Position, Quaternion.identity, Vector3.one, ref waypoints[i].Angles[0].AngleVector, ref waypoints[i].Angles[1].AngleVector);
                            }
                        }
                        Handles.color = Color.red;
                        if (positions.Count > 0)
                            Handles.DrawAAPolyLine(10, positions.Concat(new Vector3[] { positions[0] }).ToArray());
                        Handles.color = Color.white;
                    }
                    EditorUtility.SetDirty(selectedEnemy);

                }

            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("LEVEL EDITOR", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            levelName = EditorGUILayout.TextField("Scene Name:", levelName);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE"))
            {
                if (!string.IsNullOrEmpty(levelName))
                {
                    LevelSerializer.Serialize(levelName);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }


        void DisplayMenuItem(string ItemName)
        {
            if (GUILayout.Button(ItemName))
            {
                GameObject shape = Resources.Load<GameObject>("Items/Collectables/" + ItemName);
                shape = Instantiate(shape);
                shape.name = ItemName;
                Selection.activeGameObject = shape;
                Tools.current = Tool.Move;
            }
        }

        void DisplayPrefabs(string ItemName, Action<Transform> action = null)
        {

            if (GUILayout.Button(ItemName))
            {
                Debug.Log(ItemName);
                GameObject shape = Resources.Load<GameObject>("Prefabs/" + ItemName);
                shape = Instantiate(shape);
                shape.name = ItemName;
                Selection.activeGameObject = shape;
                Tools.current = Tool.Move;
                action?.Invoke(shape.transform);
            }
        }



        static JointAngularLimitHandle jointAngularLimitHandle = new JointAngularLimitHandle();

        public static void JointAngularLimitHandle(Vector3 center, Quaternion rotation, Vector3 size, ref Vector3 minAngles, ref Vector3 maxAngles) { JointAngularLimitHandle(center, rotation, size, ref minAngles, ref maxAngles, jointAngularLimitHandle.xHandleColor, jointAngularLimitHandle.yHandleColor, jointAngularLimitHandle.zHandleColor); }
        public static void JointAngularLimitHandle(Vector3 center, Quaternion rotation, Vector3 size, ref Vector3 minAngles, ref Vector3 maxAngles, Color xHandleColor, Color yHandleColor, Color zHandleColor)
        {
            Matrix4x4 trs = Matrix4x4.TRS(center, rotation, size);

            using (new Handles.DrawingScope(trs))
            {
                jointAngularLimitHandle.xHandleColor = xHandleColor;
                jointAngularLimitHandle.yHandleColor = yHandleColor;
                jointAngularLimitHandle.zHandleColor = zHandleColor;

                jointAngularLimitHandle.xMin = minAngles.x;
                jointAngularLimitHandle.yMin = minAngles.y;
                jointAngularLimitHandle.zMin = minAngles.z;
                jointAngularLimitHandle.xMax = maxAngles.x;
                jointAngularLimitHandle.yMax = maxAngles.y;
                jointAngularLimitHandle.zMax = maxAngles.z;

                jointAngularLimitHandle.DrawHandle();

                minAngles.x = jointAngularLimitHandle.xMin;
                minAngles.y = jointAngularLimitHandle.yMin;
                minAngles.z = jointAngularLimitHandle.zMin;
                maxAngles.x = jointAngularLimitHandle.xMax;
                maxAngles.y = jointAngularLimitHandle.yMax;
                maxAngles.z = jointAngularLimitHandle.zMax;
            }
        }
    }


}