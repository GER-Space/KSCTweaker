using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSCTweaker
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KSCTweaker : MonoBehaviour
    {

        internal static GameObject desterLights;
        bool pimpLV2Runway = false;

        public void Awake()
        {
            Log.Normal("started");

            DontDestroyOnLoad(this);

            ModifyDishes();

            if (Expansions.ExpansionsLoader.IsExpansionInstalled("MakingHistory"))
            {
                GameEvents.onGameSceneSwitchRequested.Add(OnSceneSwitchReq);
                GameEvents.onLevelWasLoaded.Add(OnLevelWasLoad);
                desterLights = GetDesterRunwayLights();
                DontDestroyOnLoad(desterLights);
            }

        }

        public void Destroy()
        {
            if (Expansions.ExpansionsLoader.IsExpansionInstalled("MakingHistory"))
            {
                GameEvents.onGameSceneSwitchRequested.Remove(OnSceneSwitchReq);
                GameEvents.onLevelWasLoaded.Remove(OnLevelWasLoad);
            }
        }


        public static GameObject GetDesterRunwayLights()
        {

            List<string> allStatics = new List<string> { "Model", "" };


            foreach (PQSCity2 pqs2 in Resources.FindObjectsOfTypeAll<PQSCity2>())
            {

                if (pqs2.gameObject == null)
                    continue;

                if (pqs2.gameObject.name != "Desert_Airfield")
                {
                    continue;
                }

                Log.Normal("found PQS2 " + pqs2.gameObject.name);

                Transform lights1 = pqs2.gameObject.transform.FindRecursive("Section1_lights");
                return lights1.gameObject;
            }
            return null;
        }



        internal void ModifyDishes()
        {
            foreach (var facility in Resources.FindObjectsOfTypeAll<Upgradeables.UpgradeableObject>())
            {
                for (int i = 0; i < facility.UpgradeLevels.Length; i++)
                {
                    if (facility.name == "TrackingStation")
                    {
                        if (i < 2)
                        {
                            Log.Normal("Found Facility: " + facility.name + "_lv_" + (i + 1).ToString());
                            FixDish(facility.UpgradeLevels[i].facilityPrefab);
                        }
                    }
                }
            }
        }


        public void OnSceneSwitchReq(GameEvents.FromToAction<GameScenes, GameScenes> fromTo)
        {
            if (fromTo.to == GameScenes.SPACECENTER && fromTo.from == GameScenes.MAINMENU)
            {
                pimpLV2Runway = true;
            }
        }


        void OnLevelWasLoad(GameScenes scene)
        {

            if (scene == GameScenes.SPACECENTER)
            {
                if (pimpLV2Runway)
                {
                    PimpLevel2Runway();
                    pimpLV2Runway = false;
                }
            }

        }

        internal void FixDish(GameObject facility)
        {
            List<DishController.Dish> dishes = new List<DishController.Dish>();

            DishController controller = facility.AddComponent<DishController>();

            controller.fakeTimeWarp = 1f;
            controller.maxSpeed = 10f;
            controller.maxElevation = 20f;
            controller.minElevation = -70f;

            foreach (Transform dishTransform in facility.transform.FindAllRecursive("TS_dish"))
            {
                Log.Normal("Dish: Found Dish");
                DishController.Dish dish = new DishController.Dish();

                dish.elevationTransform = dishTransform.FindRecursive("dish_antenna");
                //dish.elevationInit = new Quaternion();
                dish.rotationTransform = dishTransform.FindRecursive("dish_support");

                dish.elevationTransform.parent = dish.rotationTransform;

                dishes.Add(dish);

            }
            controller.dishes = dishes.ToArray();
            controller.enabled = true;
        }



        internal void PimpLevel2Runway()
        {
            if (ScenarioUpgradeableFacilities.GetFacilityLevel("Runway") == 0.5f)
            {
                PQSCity kscPQS = GethomeWorld().pqsController.transform.GetComponentsInChildren<PQSCity>(true).Where(x => x.name == "KSC").FirstOrDefault();

                if (kscPQS == null)
                {
                    Log.Error("Cannot find KSC");
                    return;
                }

                GameObject runway = kscPQS.gameObject.transform.FindRecursive("Runway").gameObject;
                if (runway != null)
                {
                    Log.Normal("runway Found");
                    IEnumerator coroutine = DelayedPatcher(runway);
                    StartCoroutine(coroutine);
                }
            }
        }


        internal IEnumerator DelayedPatcher(GameObject runway)
        {
            yield return new WaitForSeconds(2);

            PimpLv2Runway(runway, true);
        }



        internal void PimpLv2Runway(GameObject modelPrefab, bool state = false)
        {
            Log.Normal("Prefab name: " + modelPrefab.name);
            int count = 0;
            foreach (Transform target in modelPrefab.transform.FindAllRecursive("fxTarget"))
            {
                GameObject light = GameObject.Instantiate(desterLights);
                light.SetActive(state);
                Log.Normal("found target: " + target.parent.name);
                light.transform.parent = target.parent.FindRecursiveContains("runway");
                light.transform.localScale *= 0.6f;

                light.transform.rotation = modelPrefab.transform.rotation;

                light.transform.localPosition = new Vector3(6.5f, 0.85f, -1050f + count * 330);

                light.name = light.transform.parent.name + "_lights";

                count++;
            }
        }

        internal static CelestialBody GethomeWorld()
        {

            CelestialBody[] bodies = FlightGlobals.Bodies.ToArray();
            foreach (CelestialBody body in bodies)
            {
                if (body.isHomeWorld)
                {
                    //Log.Normal("returning: " + body.name);
                    return body;
                }

            }
            Log.UserError("No Homeworld found");
            return null;

        }

    }
}
