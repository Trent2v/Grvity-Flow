//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

using UnityEditor;

namespace DigitalOpus.MB.Core{
	
	public interface MB3_MeshBakerEditorWindowInterface{
		MonoBehaviour target{
			get;
			set;
		}	
	}
	
	public class MB3_MeshBakerEditorInternal{
		//add option to exclude skinned mesh renderer and mesh renderer in filter
		//example scenes for multi material
		private static GUIContent
			outputOptoinsGUIContent = new GUIContent("Output", ""),
			openToolsWindowLabelContent = new GUIContent("Open Tools For Adding Objects", "Use these tools to find out what can be combined, discover problems with meshes, and quickly add objects."),
			renderTypeGUIContent = new GUIContent("Renderer","The type of renderer to add to the combined mesh."),
			objectsToCombineGUIContent = new GUIContent("Custom List Of Objects To Be Combined","You can add objects here that were not on the list in the MB3_TextureBaker as long as they use a material that is in the Material Bake Results"),
			textureBakeResultsGUIContent = new GUIContent("Material Bake Result","When materials are combined a MB2_TextureBakeResult Asset is generated. Drag that Asset to this field to use the combined material."),
			useTextureBakerObjsGUIContent = new GUIContent("Same As Texture Baker","Build a combined mesh using using the same list of objects that generated the Combined Material"),
			lightmappingOptionGUIContent = new GUIContent("Lightmapping UVs","preserve current lightmapping: Use this if all source objects are lightmapped and you want to preserve it. All source objects must use the same lightmap\n\n"+
																			 "generate new UV Layout: Use this if you want to bake a lightmap after the combined mesh has been generated\n\n" +
																			 "copy UV2 unchanged: Use this if UV2 is being used for something other than lightmaping.\n\n" + 
																			 "ignore UV2: A UV2 channel will not be generated for the combined mesh"),
			combinedMeshPrefabGUIContent = new GUIContent("Combined Mesh Prefab","Create a new prefab asset an drag an empty game object to it. Drag the prefab asset to here."),
			clearBuffersAfterBakeGUIContent = new GUIContent("Clear Buffers After Bake","Frees memory used by the MeshCombiner. Set to false if you want to update the combined mesh at runtime."),
			doNormGUIContent = new GUIContent("Include Normals"),
			doTanGUIContent = new GUIContent("Include Tangents"),
			doColGUIContent = new GUIContent("Include Colors"),
			doUVGUIContent = new GUIContent("Include UV"),
			doUV3GUIContent = new GUIContent("Include UV3"),
            doUV4GUIContent = new GUIContent("Include UV4");

        private SerializedObject meshBaker;
		private SerializedProperty  lightmappingOption, combiner, outputOptions, textureBakeResults, useObjsToMeshFromTexBaker, renderType, fixOutOfBoundsUVs, objsToMesh, mesh;
		private SerializedProperty doNorm, doTan, doUV, doUV3, doUV4, doCol, clearBuffersAfterBake;
		bool showInstructions = false;
		bool showContainsReport = true;
		
		void _init (MB3_MeshBakerCommon target) {
			meshBaker = new SerializedObject(target);
			objsToMesh = meshBaker.FindProperty("objsToMesh");
			combiner = meshBaker.FindProperty("_meshCombiner");
			outputOptions = combiner.FindPropertyRelative("_outputOption");			
			renderType = combiner.FindPropertyRelative("_renderType");
			useObjsToMeshFromTexBaker = meshBaker.FindProperty("useObjsToMeshFromTexBaker");
			textureBakeResults = combiner.FindPropertyRelative("_textureBakeResults");
			lightmappingOption = combiner.FindPropertyRelative("_lightmapOption");
			doNorm = combiner.FindPropertyRelative("_doNorm");
			doTan = combiner.FindPropertyRelative("_doTan");
			doUV = combiner.FindPropertyRelative("_doUV");
			doUV3 = combiner.FindPropertyRelative("_doUV3");
            doUV4 = combiner.FindPropertyRelative("_doUV4");
            doCol = combiner.FindPropertyRelative("_doCol");
			clearBuffersAfterBake = meshBaker.FindProperty("clearBuffersAfterBake");
			mesh = combiner.FindPropertyRelative("_mesh");
		}	
		
		public void OnInspectorGUI(MB3_MeshBakerCommon target, System.Type editorWindowType){
			DrawGUI(target, editorWindowType);
		}
		
		public void DrawGUI(MB3_MeshBakerCommon target, System.Type editorWindowType){
			if (meshBaker == null){
				_init(target);
			}
			
			meshBaker.Update();
	
			showInstructions = EditorGUILayout.Foldout(showInstructions,"Instructions:");
			if (showInstructions){
				EditorGUILayout.HelpBox("1. Bake combined material(s).\n\n" +
										"2. If necessary set the 'Material Bake Results' field.\n\n" +
										"3. Add scene objects or prefabs to combine or check 'Same As Texture Baker'. For best results these should use the same shader as result material.\n\n" +
										"4. Select options and 'Bake'.\n\n" +
										"6. Look at warnings/errors in console. Decide if action needs to be taken.\n\n" +
										"7. (optional) Disable renderers in source objects.", UnityEditor.MessageType.None);
				
				EditorGUILayout.Separator();
			}				
			
			MB3_MeshBakerCommon mom = (MB3_MeshBakerCommon) target;
			
			mom.meshCombiner.LOG_LEVEL = (MB2_LogLevel) EditorGUILayout.EnumPopup("Log Level", mom.meshCombiner.LOG_LEVEL);
			
			EditorGUILayout.PropertyField(textureBakeResults, textureBakeResultsGUIContent);
			if (textureBakeResults.objectReferenceValue != null){
				showContainsReport = EditorGUILayout.Foldout(showContainsReport, "Shaders & Materials Contained");
				if (showContainsReport){
					EditorGUILayout.HelpBox(((MB2_TextureBakeResults)textureBakeResults.objectReferenceValue).GetDescription(), MessageType.Info);	
				}
			}
			
			EditorGUILayout.LabelField("Objects To Be Combined",EditorStyles.boldLabel);
			if (mom.GetTextureBaker() != null){
				EditorGUILayout.PropertyField(useObjsToMeshFromTexBaker, useTextureBakerObjsGUIContent);
			} else {
				useObjsToMeshFromTexBaker.boolValue = false;
				mom.useObjsToMeshFromTexBaker = false;
				GUI.enabled = false;
				EditorGUILayout.PropertyField(useObjsToMeshFromTexBaker, useTextureBakerObjsGUIContent);
				GUI.enabled = true;
			}
			
			if (!mom.useObjsToMeshFromTexBaker){
				if (GUILayout.Button(openToolsWindowLabelContent)){
					MB3_MeshBakerEditorWindowInterface mmWin = (MB3_MeshBakerEditorWindowInterface) EditorWindow.GetWindow(editorWindowType);
					mmWin.target = (MB3_MeshBakerRoot) target;
				}	
				EditorGUILayout.PropertyField(objsToMesh,objectsToCombineGUIContent, true);
			} else {
				GUI.enabled = false;
				EditorGUILayout.PropertyField(objsToMesh,objectsToCombineGUIContent, true);
				GUI.enabled = true;
			}
			
			EditorGUILayout.LabelField("Output",EditorStyles.boldLabel);
			if (mom is MB3_MultiMeshBaker){
				MB3_MultiMeshCombiner mmc = (MB3_MultiMeshCombiner) mom.meshCombiner;
				mmc.maxVertsInMesh = EditorGUILayout.IntField("Max Verts In Mesh", mmc.maxVertsInMesh);	
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(doNorm,doNormGUIContent);
			EditorGUILayout.PropertyField(doTan,doTanGUIContent);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(doUV,doUVGUIContent);
			EditorGUILayout.PropertyField(doUV3,doUV3GUIContent);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(doUV4, doUV4GUIContent);
            EditorGUILayout.PropertyField(doCol,doColGUIContent);

            if (mom.meshCombiner.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping) {
                if (MBVersion.GetMajorVersion() == 5) {
                    EditorGUILayout.HelpBox("The best choice for Unity 5 is to Ignore_UV2 or Generate_New_UV2 layout. Unity's baked GI will create the UV2 layout it wants. See manual for more information.", MessageType.Warning);
                }
            }
			if (mom.meshCombiner.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout){
				EditorGUILayout.HelpBox("Generating new lightmap UVs can split vertices which can push the number of vertices over the 64k limit.",MessageType.Warning);
			}
			EditorGUILayout.PropertyField(lightmappingOption,lightmappingOptionGUIContent);
			
			EditorGUILayout.PropertyField(outputOptions,outputOptoinsGUIContent);
			EditorGUILayout.PropertyField(renderType, renderTypeGUIContent);
			if (mom.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoSceneObject){
				//todo switch to renderer
				mom.meshCombiner.resultSceneObject = (GameObject) EditorGUILayout.ObjectField("Combined Mesh Object", mom.meshCombiner.resultSceneObject, typeof(GameObject), true);
				if (mom is MB3_MeshBaker){
					string l = "Mesh";
					if (mesh.objectReferenceValue != null) l += " ("+ mesh.objectReferenceValue.GetInstanceID() +")";
					EditorGUILayout.PropertyField(mesh,new GUIContent(l));
				}
			} else if (mom.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoPrefab){
				mom.resultPrefab = (GameObject) EditorGUILayout.ObjectField(combinedMeshPrefabGUIContent, mom.resultPrefab, typeof(GameObject), true);			
				if (mom is MB3_MeshBaker){
					string l = "Mesh";
					if (mesh.objectReferenceValue != null) l += " ("+ mesh.objectReferenceValue.GetInstanceID() +")";
					EditorGUILayout.PropertyField(mesh,new GUIContent(l));
				}
			} else if (mom.meshCombiner.outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace){
				EditorGUILayout.HelpBox("NEW! Try the BatchPrefabBaker component. It makes preparing a batch of prefabs for static/ dynamic batching much easier.",MessageType.Info);
				if (GUILayout.Button("Choose Folder For Bake In Place Meshes") ){
					string newFolder = EditorUtility.SaveFolderPanel("Folder For Bake In Place Meshes", Application.dataPath, "");	
					if (!newFolder.Contains(Application.dataPath)) Debug.LogWarning("The chosen folder must be in your assets folder.");
					mom.bakeAssetsInPlaceFolderPath = "Assets" + newFolder.Replace(Application.dataPath, "");
				}
				EditorGUILayout.LabelField("Folder For Meshes: " + mom.bakeAssetsInPlaceFolderPath);
			}
			
			EditorGUILayout.PropertyField(clearBuffersAfterBake, clearBuffersAfterBakeGUIContent);
			if (GUILayout.Button("Bake")){
				bake(mom);
			}
	
			string enableRenderersLabel;
			bool disableRendererInSource = false;
			if (mom.GetObjectsToCombine().Count > 0){
				Renderer r = MB_Utility.GetRenderer(mom.GetObjectsToCombine()[0]);
				if (r != null && r.enabled) disableRendererInSource = true;
			}
			if (disableRendererInSource){
				enableRenderersLabel = "Disable Renderers On Source Objects";
			} else {
				enableRenderersLabel = "Enable Renderers On Source Objects";
			}
			if (GUILayout.Button(enableRenderersLabel)){
				mom.EnableDisableSourceObjectRenderers(!disableRendererInSource);
			}	
			
			meshBaker.ApplyModifiedProperties();		
			meshBaker.SetIsDifferentCacheDirty();
		}
			
		public static void updateProgressBar(string msg, float progress){
			EditorUtility.DisplayProgressBar("Combining Meshes", msg, progress);
		}
			
		public static bool bake(MB3_MeshBakerCommon mom){
			bool createdDummyTextureBakeResults = false;
			bool success = false;
			try{
				if (mom.textureBakeResults == null){
					if (_OkToCreateDummyTextureBakeResult(mom)){
						createdDummyTextureBakeResults = true;
						List<GameObject> gos = mom.GetObjectsToCombine();
						mom.textureBakeResults = MB2_TextureBakeResults.CreateForMaterialsOnRenderer(MB_Utility.GetRenderer(gos[0]));
					}
				}
				if (mom.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoSceneObject){
					success = MB3_MeshBakerEditorFunctions.BakeIntoCombined(mom);
				} else if (mom.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoPrefab){
					success = MB3_MeshBakerEditorFunctions.BakeIntoCombined(mom);
				} else {
					if (mom is MB3_MeshBaker){
						if (MB3_MeshCombiner.EVAL_VERSION){
							Debug.LogError("Bake Meshes In Place is disabled in the evaluation version.");
						} else {
							MB2_ValidationLevel vl = Application.isPlaying ? MB2_ValidationLevel.quick : MB2_ValidationLevel.robust; 
							if (!MB3_MeshBakerRoot.DoCombinedValidate(mom, MB_ObjsToCombineTypes.prefabOnly, new MB3_EditorMethods(), vl)) return false;
								
							List<GameObject> objsToMesh = mom.GetObjectsToCombine();
							Mesh m = MB3_BakeInPlace.BakeMeshesInPlace((MB3_MeshCombinerSingle)((MB3_MeshBaker)mom).meshCombiner, objsToMesh, mom.bakeAssetsInPlaceFolderPath, updateProgressBar);
							if (m != null) success = true;
						}
					} else {
						Debug.LogError("Multi-mesh Baker components cannot be used for Bake In Place. Use an ordinary Mesh Baker object instead.");	
					}
				}
				if (mom.clearBuffersAfterBake){
					mom.meshCombiner.ClearBuffers ();
				}
			} catch(Exception e){
				Debug.LogError(e);	
			} finally {
				if (createdDummyTextureBakeResults){
					GameObject.DestroyImmediate(mom.textureBakeResults);
					mom.textureBakeResults = null;
				}
				EditorUtility.ClearProgressBar();
			}
			return success;
		}

		public static bool _OkToCreateDummyTextureBakeResult(MB3_MeshBakerCommon mom){
			List<GameObject> objsToMesh = mom.GetObjectsToCombine ();
			if (objsToMesh.Count == 0)
				return false;
			if (objsToMesh [0] == null)
				return false;
			Material[] ms = MB_Utility.GetGOMaterials(objsToMesh[0]);
			for (int i = 1; i < objsToMesh.Count; i++) {
				Material[] ms2 = MB_Utility.GetGOMaterials(objsToMesh[i]);
				if (!MB_Utility.ArrayBIsSubsetOfA(ms,ms2)){
					Debug.LogError ("Materials on " + objsToMesh[i] + " in the list of objects to combine were not a subset of the materials on the first object in the list.");
					return false;
				}
			}
			return true;
		}
	}	
}
