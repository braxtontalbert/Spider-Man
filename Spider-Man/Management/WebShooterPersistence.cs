using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Spider_Man.Bosses.GreenGoblin;
using Spider_Man.Webshooter;
using ThunderRoad;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Spider_Man.Management
{
    public class WebShooterPersistence : ThunderScript
    {
        public static WebShooterPersistence local;
        public string characterID;
        public Dictionary<String, bool> sideData;
        private bool waitingForData = false;
        
        public override void ScriptLoaded(ModManager.ModData modData)
        {
            if (local == null) local = this;
            GameManager.local.StartCoroutine(GetJsonData());
            EventManager.onLevelLoad += LevelLoadEvent;
            EventManager.onLevelUnload += LevelUnloadEvent;
            Application.quitting += OnApplicationQuit;
            base.ScriptLoaded(modData);
            Player.onSpawn += player =>
            {
                player.onCreaturePossess += creature =>
                {
                    if (!sideData.IsNullOrEmpty())
                    {
                        foreach (KeyValuePair<string,bool> pair in sideData)
                        {
                            if (pair.Value)
                            {
                                Side.TryParse(pair.Key, out Side side);
                                var hand = creature.GetHand(side);
                                Catalog.GetData<ItemData>("Webshooter").SpawnAsync(callback =>
                                {
                                    var snapCheck = callback.gameObject.GetComponent<SnapCheck>();
                                    snapCheck.Snap(hand, callback);
                                });
                            }
                        }
                    }
                    
                    Catalog.InstantiateAsync("goblinGlider", Vector3.zero, Quaternion.identity, null, go =>
                    {
                        go.transform.position = Player.currentCreature.transform.position + Vector3.up * 2f;
                        go.transform.rotation = Quaternion.LookRotation(Player.currentCreature.transform.forward);
                        go.AddComponent<GliderController>();
                    }, "GoblinGliderHandler");
                };
                    
            };
                
        }
        
        private void LevelUnloadEvent(LevelData leveldata, LevelData.Mode mode, EventTime eventtime)
        {
            if (eventtime == EventTime.OnStart)
            {
                GameManager.local.StartCoroutine(this.SaveJsonData(Player.characterData.ID));
            }
        }

        private void OnApplicationQuit()
        {
            GameManager.local.StartCoroutine(this.SaveJsonData(Player.characterData.ID));
        }
        
        private void LevelLoadEvent(LevelData leveldata, LevelData.Mode mode, EventTime eventtime)
        {
            if (eventtime == EventTime.OnStart)
            {
                if(!waitingForData) GameManager.local.StartCoroutine(GetJsonData());
            }
        }
        
        public IEnumerator SaveJsonData(String characterID)
        {
            var dictionary = new Dictionary<string, bool>();
            dictionary.Add("Left", ManageAutoAlignment.local.left.itemAttached);
            dictionary.Add("Right", ManageAutoAlignment.local.right.itemAttached);
            PersistenceMapper mapper = new PersistenceMapper();
            mapper.characterID = characterID;
            mapper.sideData = dictionary;

            var json = mapper.ToJson();
            PlatformBase.Save save = new PlatformBase.Save(mapper.characterID, "WebShooterPersistenceData", json);
            yield return GameManager.platform.WriteSaveCoroutine(save);
        }
        
        public IEnumerator GetJsonData()
        {
            waitingForData = true;
            yield return new WaitUntil(() => Player.characterData != null);
            waitingForData = false;
            List<PlatformBase.Save> saves = null;
            List<PlatformBase.Save> characterSaves = null;
            yield return GameManager.platform.ReadSavesCoroutine("WebShooterPersistenceData", value =>
            {
                saves = value;
            });

            yield return GameManager.platform.ReadSavesCoroutine("chr", value => { characterSaves = value; });

            yield return GameManager.local.StartCoroutine(RemoveUnusedCharacterData(characterSaves, saves));

            yield return GameManager.platform.ReadSavesCoroutine("WebShooterPersistenceData", value =>
            {
                saves = value;
            });

            Debug.Log(saves);
            foreach (var save in saves)
            {
                if (save.id.Equals(Player.characterData.ID))
                {
                    PersistenceMapper mapper = JsonConvert.DeserializeObject<PersistenceMapper>(save.data);
                    if (mapper != null)
                    {
                        characterID = mapper.characterID;
                        sideData = mapper.sideData;
                        break;
                    }
                }
            }
        }
        
        IEnumerator RemoveUnusedCharacterData(List<PlatformBase.Save> characterSave, List<PlatformBase.Save> webShooterSaves)
        {
            var matchingSaves = characterSave.Select(i => i.id).Intersect(webShooterSaves.Select(i => i.id)).ToList();

            var nonMatchingSaves = new List<PlatformBase.Save>();

            foreach (var spellSave in webShooterSaves)
            {
                if (!matchingSaves.Contains(spellSave.id))
                {
                    nonMatchingSaves.Add(spellSave);
                }
            }

            yield return GameManager.platform.DeleteSaveCoroutine(nonMatchingSaves.ToArray());
        }
    }
}