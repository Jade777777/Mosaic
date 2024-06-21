using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularCharacter
{
    public class ModelGenerator
    {
        private Dictionary<ModelType,List<CModel>> _sortedModels = new();
        public ModelGenerator(List<CModel> characterModels)
        {
            _sortedModels = new();
            foreach (CModel model in characterModels)
            {
                AddModel(model);
            }
        }

        public void AddModel(CModel model)
        {
            _sortedModels.TryAdd(model.Type,new());
            _sortedModels[model.Type].Add(model);
            _sortedModels[model.Type].Sort((x,y) => x.Priority.CompareTo(y.Priority));
        }
        public void RemoveModel(CModel model)
        {
            _sortedModels[model.Type].Remove(model);
        }


        //public GameObject InstantiateModelSkeleton(Transform parent)
        //{
        //    foreach (KeyValuePair<ModelType, List<CModel>> sortedType in sortedModels)
        //    {
        //        GameObject model = sortedType.Value[0].GetModel();//the first value always has the highest priority

        //        GameObject firstSkeleton = GameObject.Instantiate(model.transform.GetChild(0).gameObject, parent);

        //        return firstSkeleton;
        //    }
        //    return null;
        //}


        public GameObject AttachCharacterModel(Transform targetRootBone, Transform targetSMRContainer)
        {

            foreach(Transform child in targetSMRContainer)
            {
                GameObject.Destroy(child.gameObject);
            }

            foreach (KeyValuePair<ModelType, List<CModel>> sortedType in _sortedModels)
            {
                GameObject modelPrefab = sortedType.Value[0].Model;//the first value always has the highest priority
                GameObject modelInstance = GameObject.Instantiate(modelPrefab);
                ReattachAllMeshRenderers(modelInstance, targetRootBone, targetSMRContainer);
                GameObject.Destroy(modelInstance);
            }
            return null;
        }



        private static void ReattachAllMeshRenderers(GameObject model, Transform targetRootBone, Transform targetSMRContainer)
        {
            SkinnedMeshRenderer[] meshes = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer mesh in meshes)
            {
                ReattachSkinnedMeshRenderer(mesh, targetRootBone, targetSMRContainer);
            }
        }

        private static void ReattachSkinnedMeshRenderer(SkinnedMeshRenderer smr, Transform targetRootBone, Transform targetSMRContainer)
        {
            smr.transform.SetParent(targetSMRContainer);
            smr.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);


            Transform[] newBones = new Transform[smr.bones.Length];

            Dictionary<string, Transform> boneMap = GetAllBonesFromSMR(targetSMRContainer);

            for (int i = 0; i < smr.bones.Length; ++i)
            {
                string bone = smr.bones[i].name;
                if (!boneMap.TryGetValue(bone, out newBones[i]))
                {
                    Debug.Log("Unable to map bone \"" + bone + "\" to target skeleton.");
                }
            }

            smr.bones = newBones;
            smr.rootBone = targetRootBone;
        }



        private static Dictionary<string, Transform> GetAllBonesFromSMR(Transform targetSMRContainer)
        {
            SkinnedMeshRenderer[] renderers = targetSMRContainer.GetComponentsInChildren<SkinnedMeshRenderer>();
            Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

            foreach (SkinnedMeshRenderer smr in renderers)
            {
                foreach (Transform bone in smr.bones)
                {
                    if (!boneMap.ContainsKey(bone.name)) boneMap[bone.name] = bone;
                }
            }
            return boneMap;
        }


    }
}