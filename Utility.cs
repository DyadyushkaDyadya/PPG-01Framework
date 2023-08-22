using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Reflection;
using System.Globalization;
using System.IO;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;

namespace Utility01
{
    public static class Truster
    {
        public static void Trust(ModMetaData modMetaData)
        {
            Type trustedCache = Type.GetType("TrustCache, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            MethodInfo trustMethod = trustedCache.GetMethod("Trust", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveMethod = trustedCache.GetMethod("Save", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo isTrustedMethod = trustedCache.GetMethod("IsTrusted", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(bool)isTrustedMethod.Invoke(null, new object[] { modMetaData }))
            {
                trustMethod.Invoke(null, new object[] { modMetaData });
                saveMethod.Invoke(null, new object[] { });
            }

        }
        public async static void TrustOurMods()
        {
            if (GameObject.Find("01TrustChecker") == null)
            {
                Debug.Log($"{ModAPI.Metadata.Name} trust 01 mods");
                var checker = new GameObject("01TrustChecker");
                GameObject.DontDestroyOnLoad(checker);
                checker.AddComponent<TrusterChecker>();
                try
                {
                    string[] trustedNames = await Utils.HttpGet<string[]>("https://modsdownloader.com/api01/trusted.php");
                    var getUniqueNameMethod = typeof(ModMetaData).GetMethod("GetUniqueName", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var modMetaData in ModLoader.LoadedMods)
                    {
                        string uniqueName = (string)getUniqueNameMethod.Invoke(modMetaData, new object[] { });
                        if (trustedNames.Contains(uniqueName))
                        {
                            Trust(modMetaData);
                        }
                    }
                }
                catch { }
            }
        }
        public class TrusterChecker : MonoBehaviour
        {
            public static GameObject Main;
            private void Start()
            {
                Main = gameObject;
            }
            private void OnDisable()
            {
                try
                {
                    Destroy(gameObject);
                }
                catch
                {

                }
                var newobj = new GameObject(Main.name);
                GameObject.DontDestroyOnLoad(newobj);
                newobj.AddComponent<TrusterChecker>();
            }
        }
    }
    public static class AssHelper
    {
        private static Type m_ass;
        private static MethodInfo m_getTypeAss;
        private static MethodInfo m_fileType;
        private static MethodInfo m_load;
        static AssHelper()
        {
            m_ass = Type.GetType("System.Reflection.Assembly");
            m_getTypeAss = Type.GetType("System.Reflection.Assembly").GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(method => method.Name == "GetType" && method.GetParameters().Count() == 1 && method.GetParameters()[0].ParameterType.ToString() == "System.String").First();
            m_fileType = Type.GetType("System.IO.File").GetMethod("ReadAllBytes", BindingFlags.Public | BindingFlags.Static);
            m_load = m_ass.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.Name == "Load").Where(method => method.GetParameters()[0].ParameterType.Name == "Byte[]").First();
        }
        public static object Load(string path)
        {
            var bytes = (byte[])m_fileType.Invoke(null, new object[] { path });
            return m_load.Invoke(null, new object[] { bytes });
        }
    }
    public static class AssetBundleExtractor
    {
        private static Type m_assetBundleType;
        private static MethodInfo m_LoadFromFile;
        private static MethodInfo m_LoadFromBundle;
        private static MethodInfo m_UnloadBundle;
        static AssetBundleExtractor()
        {
            m_assetBundleType = Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule");
            m_LoadFromFile = m_assetBundleType.GetMethod("LoadFromFile", new[] { typeof(string) });
            m_LoadFromBundle = m_assetBundleType.GetMethod("LoadAsset", new[] { typeof(string), typeof(Type) });
            m_UnloadBundle = m_assetBundleType.GetMethod("Unload");
        }
        public static object LoadFromFile(string pathToBundle)
        {
            object assetBundle = m_LoadFromFile.Invoke(null, new object[] { pathToBundle });
            return assetBundle;
        }
        public static void UnloadBundle(object bundle)
        {
            m_UnloadBundle.Invoke(bundle, new object[] { false });
        }
        public static T LoadFromBundle<T>(object bundle, string nameAsset)
        {
            return (T)m_LoadFromBundle.Invoke(bundle, new object[] { nameAsset, typeof(T) });
        }
    }
    public static class Utility
    {
        static Utility()
        {
            ModMetaCache();
        }

        public static string modPath;
        public static string cultureInfo = CultureInfo.CurrentCulture.Name.ToLower();
        internal static void SimpleLiquidRegister(IDWithLiqud liquid) => ModAPI.RegisterLiquid((string)liquid.GetField("ID"), liquid);
        internal static Quaternion RotateTo(this Transform transform, Transform target) => Quaternion.Euler(0, 0, Mathf.Atan2(target.position.y - transform.position.y, target.position.x - transform.position.x) * Mathf.Rad2Deg);
        internal static Vector2 GetDirection(this Transform transform, Vector2 barrelDirection) => transform.TransformDirection(barrelDirection.x * transform.localScale.x, barrelDirection.y, transform.localPosition.z);
        internal static T[] FindTypesInWorld<T>() => Resources.FindObjectsOfTypeAll(typeof(T)) as T[];
        internal static void GlobalRegister(Modification[] modifications)
        {
            foreach (Modification modification in modifications)
                ModAPI.Register(modification);
        }
        internal static Modification CreateModification(SpawnableAsset originalItem, Sprite thumbnailOverride, Category categoryOverride, string nameOverride, string descriptionOverride, string nameToOrderByOverride, Action<GameObject> afterSpawn)
        {
            return new Modification()
            {
                OriginalItem = originalItem,
                NameOverride = nameOverride + " - TUED",
                DescriptionOverride = descriptionOverride,
                CategoryOverride = categoryOverride,
                NameToOrderByOverride = nameToOrderByOverride,
                ThumbnailOverride = thumbnailOverride,
                AfterSpawn = afterSpawn
            };
        }
        internal static LimbBehaviour GetNearestLimb(this Vector2 vector, PersonBehaviour exclude = null)
        {
            float ClosestDest = Mathf.Infinity;
            LimbBehaviour Target = null;
            foreach (LimbBehaviour targets in LimbBehaviourManager.Limbs)
            {
                if (targets.isActiveAndEnabled && targets.gameObject.activeSelf && targets.HasBrain && targets.IsConsideredAlive && targets.Person && targets.Person.IsAlive() && targets.Person != exclude)
                {
                    float distanceToEnemy = (targets.transform.position - (Vector3)vector).sqrMagnitude;
                    if (distanceToEnemy < ClosestDest)
                    {
                        ClosestDest = distanceToEnemy;
                        Target = targets;
                    }
                }
            }
            return Target;
        }
        internal static void AdvancedSpriteChange(this SpriteRenderer spriteRenderer, Sprite sprite, bool refresh = true, bool fixColliders = true)
        {
            spriteRenderer.sprite = sprite;
            if (spriteRenderer.transform.parent && spriteRenderer.transform.parent.TryGetComponent(out SpriteRenderer Pspriterender))
            {
                spriteRenderer.sortingLayerName = Pspriterender.sortingLayerName;
                spriteRenderer.sortingOrder = Pspriterender.sortingOrder + 1;
            }
            if (fixColliders) spriteRenderer.gameObject.FixColliders();
            if (spriteRenderer.TryGetComponent(out PhysicalBehaviour Phys))
            {
                Phys.RecalculateMassBasedOnSize();
                if (refresh) Phys.RefreshOutline();
            }
        }
        internal static void HealLimb(this LimbBehaviour limb)
        {
            limb.PhysicalBehaviour.BurnProgress -= limb.PhysicalBehaviour.BurnProgress * 0.01f;
            limb.SkinMaterialHandler.AcidProgress -= limb.SkinMaterialHandler.AcidProgress * 0.01f;
            if (!limb.IsZombie) limb.SkinMaterialHandler.RottenProgress -= limb.SkinMaterialHandler.RottenProgress * 0.01f;
            for (int i = 0; i < limb.SkinMaterialHandler.damagePoints.Length; i++)
                limb.SkinMaterialHandler.damagePoints[i].z *= 0.998f;
            limb.SkinMaterialHandler.Sync();
            limb.HealBone();
            limb.LungsPunctured = false;
            limb.CirculationBehaviour.HealBleeding();
            limb.CirculationBehaviour.IsPump = limb.CirculationBehaviour.WasInitiallyPumping;
            limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), (limb.CirculationBehaviour.Limits.y - limb.CirculationBehaviour.GetAmountOfBlood()) * 0.02f);
            limb.CirculationBehaviour.BloodFlow = 1;
            limb.Health = Mathf.Clamp(limb.Health += limb.InitialHealth * 0.998f * 0.006f, limb.InitialHealth * 0.1f, limb.InitialHealth * 2);
            limb.InternalTemperature = limb.BodyTemperature;
        }
        internal static SpriteRenderer CreateSpriteObject(this Transform Parent, Vector3 LocalPosition, Vector3 LocalScale, Sprite Sprite, bool ShouldGlow = false, UnityAction<GameObject> Action = null)
        {
            GameObject SpriteObject = new GameObject($"SpriteObject_{Parent.name}_{UnityEngine.Random.Range(-99999, 99999)}");
            SpriteObject.AddComponent<Optout>();
            SpriteObject.transform.SetParent(Parent);
            SpriteObject.transform.localPosition = LocalPosition;
            SpriteObject.transform.rotation = SpriteObject.transform.parent.rotation;
            SpriteObject.transform.localScale = LocalScale;
            SpriteRenderer SpriteObjectRenderer = SpriteObject.GetOrAddComponent<SpriteRenderer>();
            if (Sprite != null)
                AdvancedSpriteChange(SpriteObjectRenderer, Sprite, false);
            if (ShouldGlow)
            {
                SpriteObjectRenderer.sharedMaterial = ModAPI.FindMaterial("VeryBright");
            }
            Action?.Invoke(SpriteObject);
            return SpriteObjectRenderer;
        }
        internal static void BetterDestroy<T>(this GameObject Instance) where T : Component
        {
            foreach (Component component in Instance.GetComponents<T>())
                Destroy(component);
        }
        internal static void InvokeMultTime(int Count, Action<int> MulltAction)
        {
            for (int i = 0; i < Count; i++)
            {
                MulltAction?.Invoke(i);
            }
        }
        public static Vector3 GetModuleVector(this Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
        }
        public static void SetPositionWithOffSet(this Transform child, Transform parent, float offSetX = 0, float offSetY = 0)
        {
            child.position = parent.position;
            child.localPosition = new Vector3(offSetX, offSetY);
            child.localRotation = Quaternion.identity;
        }
        internal static void AdvancedSpriteChange(this GameObject Instance, Sprite Sprite, bool Refresh = false, bool Fixcoll = true)
        {
            SpriteRenderer spriterender = Instance.GetComponent<SpriteRenderer>();
            spriterender.sprite = Sprite;
            if (Instance.transform.parent && Instance.transform.parent.TryGetComponent(out SpriteRenderer Pspriterender))
            {
                spriterender.sortingLayerName = Pspriterender.sortingLayerName;
                spriterender.sortingOrder = Pspriterender.sortingOrder + 1;
            }
            if (Refresh) Instance.GetComponent<PhysicalBehaviour>().RefreshOutline();
            if (Fixcoll) Instance.FixColliders();
        }
        public static Vector3 BallisticVel(Vector3 target, Vector3 source, float angle)
        {
            var dir = target - source;  // get target direction
            var h = dir.y;  // get height difference
            dir.y = 0;  // retain only the horizontal direction
            var dist = dir.magnitude;  // get horizontal distance
            var a = angle * Mathf.Deg2Rad;  // convert angle to radians
            dir.y = dist * Mathf.Tan(a);  // set dir to the elevation angle
            dist += h / Mathf.Tan(a);  // correct for small height differences
                                       // calculate the velocity magnitude
            var vel = Mathf.Sqrt(dist * Physics.gravity.magnitude / Mathf.Sin(2 * a));
            var res = vel * dir.normalized;
            if (float.IsNaN(res.x)) res = new Vector3(); // Set zero if vector NaN.
            return res;
        }
        public static GameObject PlayClip(this AudioClip audioClip, Vector3 position, float volume, int scale, float deleteAfterSeconds = 10f, float min = 5, float max = 20, Transform followTo = null)
        {
            List<AudioSource> audioSources = new List<AudioSource>();
            var newObj = new GameObject("TempSource");
            newObj.transform.position = position;
            for (int i = 0; i < scale; i++)
            {
                var newSource = newObj.CreateAudioSource(min, max);
                newSource.volume = volume;
                newSource.clip = audioClip;
                Global.main.AddAudioSource(newSource);
                audioSources.Add(newSource);
            }
            foreach (var source in audioSources)
            {
                source.Play();
            }
            if (followTo != null)
            {
                var pc = newObj.AddComponent<PseudoChild>();
                pc.Parent = followTo;
                pc.ScaleSync = false;
                pc.RotationSync = false;
            }
            if (deleteAfterSeconds != -1)
            {
                InvokeAfterDelay(deleteAfterSeconds, () =>
                {
                    newObj.Destroy();
                });
            }
            return newObj;
        }
        public static Preferences GetPreferences()
        {
            var upm = Type.GetType("UserPreferenceManager, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            Preferences current = (Preferences)upm.GetField("Current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            return current;
        }
        public static Vector3 BallisticVel(Vector3 target, Vector3 source, float angle, float gravityScale)
        {
            var dir = target - source;  // get target direction
            var h = dir.y;  // get height difference
            dir.y = 0;  // retain only the horizontal direction
            var dist = dir.magnitude;  // get horizontal distance
            var a = angle * Mathf.Deg2Rad;  // convert angle to radians
            dir.y = dist * Mathf.Tan(a);  // set dir to the elevation angle
            dist += h / Mathf.Tan(a);  // correct for small height differences
                                       // calculate the velocity magnitude
            var vel = Mathf.Sqrt(dist * (Physics.gravity.magnitude * gravityScale) / Mathf.Sin(2 * a));
            var res = vel * dir.normalized;
            if (float.IsNaN(res.x)) res = new Vector3(); // Set zero if vector NaN.
            return res;
        }
        public static Vector2 Limit(this Vector2 vector2, Vector2 maxVector)
        {
            Vector2 correctedVector = new Vector2(vector2.x > maxVector.x ? maxVector.x : vector2.x, vector2.y > maxVector.y ? maxVector.y : vector2.y);
            return correctedVector;
        }
        public static Vector2 Limit(this Vector2 vector2, Vector2 minVector, Vector2 maxVector)
        {
            Vector2 correctedVector = new Vector2(vector2.x > maxVector.x ? maxVector.x : vector2.x, vector2.y > maxVector.y ? maxVector.y : vector2.y);
            correctedVector = new Vector2(correctedVector.x < minVector.x ? minVector.x : correctedVector.x, correctedVector.y < minVector.y ? minVector.y : correctedVector.y);
            return correctedVector;
        }
        public static Color GetColor(this GameObject obj)
        {
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Sprite sprite = spriteRenderer.sprite;
                if (sprite != null)
                {
                    int x = (int)sprite.rect.width / 2;
                    int y = (int)sprite.rect.height / 2;
                    return GetPixelColor(sprite, x, y);
                }
            }
            return Color.white; // возвращает основной цвет объекта если спрайт не найден
        }
        public static Color GetPixelColor(Sprite sprite, int x, int y)
        {
            Texture2D texture = sprite.texture;
            if (texture == null)
            {
                return Color.white;
            }

            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            texture = new Texture2D(texture.width, texture.height);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            int pixelX = Mathf.FloorToInt(sprite.rect.x) + x;
            int pixelY = Mathf.FloorToInt(sprite.rect.y) + y;
            if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
            {
                return texture.GetPixel(pixelX, pixelY);
            }

            return Color.white;
        }
        public static void DoOnce(string nameOperation, Action action)
        {
            if (PlayerPrefs.GetInt(nameOperation, 0) == 0)
            {
                PlayerPrefs.SetInt(nameOperation, 1);
                action.Invoke();
            }
        }
        public static void CreateNewMix(List<Liquid> liquids, float speed = 0.01f)
        {
            List<LiquidMixInstructions> mixInstructions = LiquidMixingController.MixInstructions;
            LiquidMixInstructions liquidMixInstructions = new LiquidMixInstructions
            (liquids.Where(l => l != liquids.Last()).ToArray(), liquids.Last(), speed);
            liquidMixInstructions.ContainerFilter = ((BloodContainer c) => !(c is CirculationBehaviour));
            mixInstructions.Add(liquidMixInstructions);
        }
        public static void RegisterCategory(string name, string description, Sprite icon)
        {
            CatalogBehaviour manager = UnityEngine.Object.FindObjectOfType<CatalogBehaviour>();
            if (manager.Catalog.Categories.FirstOrDefault((Category c) => c.name == name) == null)
            {
                Category category = ScriptableObject.CreateInstance<Category>();
                category.name = name;
                category.Description = description;
                category.Icon = icon;
                Category[] NewCategories = new Category[manager.Catalog.Categories.Length + 1];
                Category[] categories = manager.Catalog.Categories;
                for (int i = 0; i < categories.Length; i++)
                {
                    NewCategories[i] = categories[i];
                }
                NewCategories[NewCategories.Length - 1] = category;
                manager.Catalog.Categories = NewCategories;
            }
        }
        public static ItemButtonBehaviour GetItemButtonBehaviour(SpawnableAsset spawnableAsset)
        {
            List<ItemButtonBehaviour> itemButtonBehaviours = (List<ItemButtonBehaviour>)typeof(CatalogBehaviour).GetField("items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameObject.FindObjectOfType<CatalogBehaviour>()); // zooi <3
            return itemButtonBehaviours.Where(itemButton => itemButton.Item == spawnableAsset).FirstOrDefault();
        }
        internal static void ChangeLimbHealthBar(this LimbBehaviour Limb, Sprite Dead, Sprite Healthbar, Color Color, UnityAction<GameObject> Action = null)
        {
            GameObject LimbBar = (GameObject)typeof(LimbBehaviour).GetField("myStatus", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Limb);
            LimbBar.transform.Find("bar").gameObject.GetComponent<SpriteRenderer>().color = Color;
            LimbStatusBehaviour Status = LimbBar.GetComponent<LimbStatusBehaviour>();
            if (Healthbar) Status.BarSprite = Healthbar;
            if (Dead) Status.DeadSprite = Dead;
            Action?.Invoke(LimbBar);
        }
        internal static void ChangeLimbHealthBar(this LimbBehaviour Limb, UnityAction<GameObject> Action = null)
        {
            GameObject LimbBar = (GameObject)typeof(LimbBehaviour).GetField("myStatus", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Limb);
            LimbStatusBehaviour Status = LimbBar.GetComponent<LimbStatusBehaviour>();
            Action?.Invoke(LimbBar);
        }
        public static void AddButtonInCatalog(string name, string desc, string categoryName, Sprite sprite, int targetSibilingIndex, UnityAction action)
        {
            if (GameObject.Find("CustomButtons") == null)
            {
                new GameObject("CustomButtons");
            }
            var newButton = GameObject.Instantiate(CatalogBehaviour.Main.ItemButtonPrefab, CatalogBehaviour.Main.ItemContainer);
            newButton.GetComponent<ItemButtonBehaviour>().Destroy();
            var toolTip = newButton.GetComponent<HasTooltipBehaviour>();
            newButton.transform.Find("Outdated").gameObject.Destroy();
            newButton.transform.Find("Remove").gameObject.Destroy();
            newButton.transform.Find("button row").gameObject.SetActive(false);
            toolTip.TooltipText = CatalogBehaviour.Main.TooltipText;
            toolTip.Text = $"<b>{name}</b>\n{desc}";
            newButton.GetComponent<Image>().sprite = sprite;
            var button = newButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            //button.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
            button.onClick.AddListener(action);
            var customButton = GameObject.Find("CustomButtons").AddComponent<CustomButtonBehaviour>();
            customButton.TargetCategory = categoryName;
            customButton.TargetButton = newButton;
            newButton.transform.SetSiblingIndex(targetSibilingIndex);
        }
        public static bool IsVisible(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
            {
                if (spriteRenderer.isVisible)
                {
                    return true;
                }
            }
            else
            {
                foreach (var spriteRendererChild in gameObject.transform.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (spriteRendererChild.isVisible)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static string GetTranslatedString(string en, string ru)
        {
            return cultureInfo.Contains("ru") ? ru : en;
        }
        public static JointMotor2D SetMotorForce(JointMotor2D jointMotor2D, float force, float torque = 10000f)
        {
            jointMotor2D.motorSpeed = force;
            jointMotor2D.maxMotorTorque = torque;
            return jointMotor2D;
        }
        public static void SetExternField(Component obj, string nameField, object value)
        {
            obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().SetValue(obj, value);
        }
        public static void SetField<T>(this T obj, string nameField, object value)
        {
            typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().SetValue(obj, value);
        }
        public static object GetField<T>(this T obj, string nameField)
        {
            return typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().GetValue(obj);
        }
        public static object GetField(this Type obj, string nameField)
        {
            return obj.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().GetValue(obj);
        }
        public static FieldInfo GetFieldInfo<T>(this T obj, string nameField)
        {
            return typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault();
        }
        public static A GetField<T, A>(this T obj, string nameField)
        {
            return (A)typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().GetValue(obj);
        }
        public static void CopyPublicFields(this object ObjectB, object ObjectA)
        {
            foreach (FieldInfo fieldinfo in ObjectA.GetType().GetFields())
            {
                var sameField = ObjectB.GetType().GetField(fieldinfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                if (sameField != null && sameField.FieldType == fieldinfo.FieldType)
                {
                    sameField.SetValue(ObjectB, fieldinfo.GetValue(ObjectA));
                }
            }
        }
        public static void CopyAllFields(this object ObjectB, object ObjectA)
        {
            foreach (FieldInfo fieldinfo in ObjectA.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
            {
                var sameField = ObjectB.GetType().GetField(fieldinfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                if (sameField != null && sameField.FieldType == fieldinfo.FieldType)
                {
                    sameField.SetValue(ObjectB, fieldinfo.GetValue(ObjectA));
                }
            }
        }
        public static List<T> ToListFix<T>(this HashSet<T> hashset)
        {
            var list = new List<T>();
            foreach (var obj in hashset)
            {
                list.Add(obj);
            }
            return list;

        }
        public static PointDebuggerCircle DrawCircle(this GameObject gameObject, float radius = 0.3f)
        {
            var pointDebugger = gameObject.AddComponent<PointDebuggerCircle>();
            pointDebugger.Radius = radius;
            return pointDebugger;
        }
        public static PointDebuggerVector DrawVector(this Vector2 vector, float radius = 0.3f)
        {
            var circleHandler = new GameObject("colliderHandler");
            var pointBehaviour = circleHandler.AddComponent<PointDebuggerVector>();
            pointBehaviour.Radius = radius;
            pointBehaviour.Vector = vector;
            return pointBehaviour;
        }
        public static HingeJointDebugger DrawJoint(this HingeJoint2D hingeJoint2D, float radius = 0.01f)
        {
            var jointHandler = new GameObject("jointHandler");
            var jointDebugger = jointHandler.AddComponent<HingeJointDebugger>();
            jointDebugger.Joint = hingeJoint2D;
            jointDebugger.Radius = radius;
            return jointDebugger;
        }
        public static bool IsColissionDisabled(this GameObject gameObject)
        {
            return gameObject.layer == 10 ? true : false;
        }
        public static PointDebuggerCollider DrawCollider(this GameObject gameObject)
        {
            var a = gameObject.AddComponent<PointDebuggerCollider>();
            a.Collider = gameObject.GetComponent<Collider2D>();
            return a;
        }
        public static PointDebuggerCollider DrawCollider(this Collider2D collider)
        {
            var colliderHandler = new GameObject("colliderHandler");
            var a = colliderHandler.AddComponent<PointDebuggerCollider>();
            a.Collider = collider;
            return a;
        }
        public static void Destroy(this UnityEngine.Object @object)
        {
            try
            {
                UnityEngine.Object.Destroy(@object);
            }
            catch { }
        }
        public static Vector2 GetDirection(this Vector2 v, Vector2 other)
        {
            Vector2 direction = other - v;

            float x = direction.x == 0 ? 0 : direction.x / (Mathf.Abs(direction.x));
            float y = direction.y == 0 ? 0 : direction.y / (Mathf.Abs(direction.y));

            return new Vector2(x, y);
        }
        public static void IgnoreCollision(this GameObject[] gameObjects, bool ignore = true)
        {
            foreach (var gameObject in gameObjects)
            {
                foreach (var gameObjectToCollision in gameObjects.Where(gameObjectFilter => gameObjectFilter != gameObject))
                {
                    Physics2D.IgnoreCollision(gameObject.GetComponent<Collider2D>(), gameObjectToCollision.GetComponent<Collider2D>(), ignore);
                }
            }
        }
        public static void SetSprite(this GameObject gameObject, Sprite sprite, bool fixColliders = true, string sortingLayerName = "Default", int sortingOrder = 4)
        {
            if (!gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
            {
                gameObject.AddComponent<SpriteRenderer>();
            }
            var renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            if (fixColliders)
            {
                gameObject.FixColliders();
            }
            if (gameObject.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour physicalBehaviour))
            {
                physicalBehaviour.RefreshOutline();
            }
        }
        public static void ShowDialog(DialogBox dialogBox)
        {
            dialogBox.gameObject.SetActive(true);
        }
        public static void HideDialog(DialogBox dialogBox)
        {
            dialogBox.gameObject.SetActive(false);
        }
        public static Collider2D[] InCollider(this Collider2D collider)
        {
            List<Collider2D> colliders = new List<Collider2D>();
            collider.OverlapCollider(new ContactFilter2D(), colliders);
            return colliders.ToArray();
        }


        public static ModEntryBehaviour GetModEntryBehaviour(ModMetaData modMetaData)
        {
            try
            {
                return GameObject.FindObjectsOfType<ModEntryBehaviour>().Where(m => m.ModMeta == modMetaData)?.First();
            }
            catch
            {
                return null;
            }
        }
        public static TMP_FontAsset GetTMP_FontAsset(string nameFont)
        {
            switch (nameFont)
            {
                case "Pixel": return ModAPI.FindSpawnable("Thermometer").Prefab.transform.Find("Text").GetComponent<TextMeshPro>().font;
                case "Clock": return ModAPI.FindSpawnable("Infrared Thermometer").Prefab.transform.Find("value").GetComponent<TextMeshPro>().font;
                default: return GameObject.Find("Canvas").transform.Find("Pre-release build text").GetComponent<TextMeshProUGUI>().font;
            }
        }
        public static GameObject CreateChildObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            return gameObject;
        }
        public static GameObject CreateChildObject(GameObject prefab, Transform parent)
        {
            var gameObject = GameObject.Instantiate(prefab);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            return gameObject;
        }
        public static void InitializePhysicalComponent(this GameObject gameObject)
        {
            gameObject.layer = LayerMask.NameToLayer("Objects");
            gameObject.AddComponent<Rigidbody2D>();
            gameObject.AddComponent<SpriteRenderer>();
            gameObject.AddComponent<BoxCollider2D>();
            PhysicalBehaviour physicalBehaviour = gameObject.AddComponent<PhysicalBehaviour>();
            physicalBehaviour.Properties = ModAPI.FindPhysicalProperties("Metal");
            physicalBehaviour.SpawnSpawnParticles = false;
            gameObject.AddComponent<AudioSourceTimeScaleBehaviour>();
            physicalBehaviour.OverrideShotSounds = Array.Empty<AudioClip>();
            physicalBehaviour.OverrideImpactSounds = Array.Empty<AudioClip>();
        }
        public static void OpenLink(string url)
        {
            Type type = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule");
            type.GetMethod("OpenURL", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Invoke(null, new object[] { url });
        }
        public static Vector3 GetLocalScale(this Transform transform, Vector3 targetLossyScale)
        {
            var xDif = transform.localScale.x / transform.lossyScale.x;
            if (float.IsNaN(xDif))
            {
                xDif = 0;
            }
            var yDif = transform.localScale.y / transform.lossyScale.y;
            if (float.IsNaN(yDif))
            {
                yDif = 0;
            }
            var zDif = transform.lossyScale.z / transform.lossyScale.z;
            if (float.IsNaN(zDif))
            {
                zDif = 0;
            }
            return new Vector3(targetLossyScale.x * xDif, targetLossyScale.y * yDif, targetLossyScale.z * zDif);
        }
        public static string GetHierarchyPath(Transform startTransform, Transform endTransform)
        {
            try
            {
                string hierarchyPath = endTransform.name;
                if (endTransform.root == startTransform.root)
                {
                    Transform lastTransform = endTransform;
                    while (true)
                    {
                        if (lastTransform.parent == endTransform.root)
                        {
                            break;
                        }
                        hierarchyPath = lastTransform.parent.name + "/" + hierarchyPath;
                        lastTransform = lastTransform.parent;
                        if (lastTransform == startTransform)
                        {
                            break;
                        }
                    }
                    return hierarchyPath;
                }
                return hierarchyPath;
            }
            catch
            {
                return "";
            }
        }
        public static void ModMetaCache()
        {
            Utility.modPath = ModAPI.Metadata.MetaLocation;
        }
        public static Sprite LoadSprite(string path, bool withoutIncludeModPath = false, FilterMode filterMode = FilterMode.Point, float pixels = 35)
        {
            return Utils.LoadSprite((withoutIncludeModPath == true ? path : modPath + "\\" + path), filterMode, pixels, false);
        }
        public static SpriteRenderer GetSpriteRenderer(this GameObject gameObject)
        {
            return gameObject.GetComponent<SpriteRenderer>();
        }
        public static Texture2D LoadTexture(string path)
        {
            return Utils.LoadTexture(modPath + "\\" + path);
        }
        public static AudioClip LoadSound(string path)
        {
            return Utils.FileToAudioClip(modPath + "\\" + path);
        }
        public static void ChangeSpecificLimbSprite(this LimbBehaviour limbBehaviour, Sprite skin, Sprite flash, Sprite bone, Sprite damage)
        {
            var limbSpriteRenderer = limbBehaviour.GetComponent<SpriteRenderer>();
            limbSpriteRenderer.sprite = skin;
            limbSpriteRenderer.material.SetTexture(ShaderProperties.Get("_FleshTex"), flash.texture);
            limbSpriteRenderer.material.SetTexture(ShaderProperties.Get("_BoneTex"), bone.texture);
            limbSpriteRenderer.material.SetTexture(ShaderProperties.Get("_DamageTex"), damage.texture);
        }
        public static void ChangeSpecificLimbSprite(this SpriteRenderer spriteRenderer, Sprite skin, Sprite flash, Sprite bone, Sprite damage)
        {
            spriteRenderer.sprite = skin;
            spriteRenderer.material.SetTexture(ShaderProperties.Get("_FleshTex"), flash.texture);
            spriteRenderer.material.SetTexture(ShaderProperties.Get("_BoneTex"), bone.texture);
            spriteRenderer.material.SetTexture(ShaderProperties.Get("_DamageTex"), damage.texture);
        }
        public static GameObject FindPrefab(string name)
        {
            return Resources.Load<GameObject>(name);
        }
        public static void UpdateOutline(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour physicalBehaviour))
            {
                physicalBehaviour.RefreshOutline();
            }
        }
        public static void ErrorNotify(string message)
        {
            ModAPI.Notify($"<color=\"red\">{message}");
        }
        public static void SuccessfulNotify(string message)
        {
            ModAPI.Notify($"<color=\"green\">{message}");
        }
        public static PhysicalProperties GetPhysicalProperties(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour physicalBehaviour))
            {
                return physicalBehaviour.Properties;
            }
            else
            {
                return null;
            }
        }
        public static bool IsInsideCollider2D(this Collider2D collider2D, GameObject gameObject)
        {
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Bounds rendererBounds = spriteRenderer.bounds;
                Vector3[] corners = new Vector3[4];
                corners[0] = new Vector3(rendererBounds.min.x, rendererBounds.min.y, 0);
                corners[1] = new Vector3(rendererBounds.min.x, rendererBounds.max.y, 0);
                corners[2] = new Vector3(rendererBounds.max.x, rendererBounds.max.y, 0);
                corners[3] = new Vector3(rendererBounds.max.x, rendererBounds.min.y, 0);

                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i] = gameObject.transform.TransformPoint(corners[i]);
                }

                rendererBounds = new Bounds(corners[0], Vector3.zero);
                for (int i = 1; i < corners.Length; i++)
                {
                    rendererBounds.Encapsulate(corners[i]);
                }

                Collider2D[] colliders = Physics2D.OverlapAreaAll(rendererBounds.min, rendererBounds.max);

                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] == collider2D)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }
        public static PhysicalBehaviour GetPhysicalBehaviour(this GameObject gameObject)
        {
            return gameObject.GetComponent<PhysicalBehaviour>();
        }
        public static float GetAverageImpulse(this Collision2D collision)
        {
            var averageImpulse = Utils.GetAverageImpulse(collision.contacts, collision.contacts.Length);
            return averageImpulse;
        }
        public static void CreateFolder(string path)
        {
            Type.GetType("System.IO.Directory").GetMethods().Where(method => method.Name == "CreateDirectory").FirstOrDefault().Invoke(null, new object[] { path });
        }
        public static bool IsFolderExists(string path)
        {
            return (bool)Type.GetType("System.IO.Directory").GetMethods().Where(method => method.Name == "Exists").FirstOrDefault().Invoke(null, new object[] { path });
        }
        public static string[] GetFileSystemEntriesFolder(string path)
        {
            return (string[])Type.GetType("System.IO.Directory").GetMethods().Where(method => method.Name == "GetFileSystemEntries").FirstOrDefault().Invoke(null, new object[] { path });
        }
        internal static void NoChildCollide(this GameObject instance)
        {
            Collider2D[] componentsInChildren = instance.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D collider2D in componentsInChildren)
            {
                foreach (Collider2D collider2D2 in componentsInChildren)
                {
                    if (collider2D && collider2D2 && collider2D != collider2D2)
                    {
                        Physics2D.IgnoreCollision(collider2D, collider2D2);
                    }
                }
            }
        }
        public static void InvokeOnStart(this GameObject gameObject, Action action)
        {
            var invoker = gameObject.AddComponent<InvokerOnStart>().ActionForInvoke = action;
        }
        public static void InvokeAfterDelay(float delay, Action action)
        {
            var invoker = new GameObject("delayer").AddComponent<InvokerAfterDelay>();
            invoker.Delay = delay;
            invoker.ActionForInvoke = action;
        }
        public static AudioSource CreateAudioSource(this GameObject gameObject, float minDistance = 5, float maxDistance = 30)
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            return audioSource;
        }
        public static Color MixColor(this Color color, Color additiveColor)
        {
            return new Color((color.r + additiveColor.r) / 2, (color.g + additiveColor.g) / 2, (color.b + additiveColor.b) / 2, (color.a + additiveColor.a) / 2);
        }
        public static GameObject CreatePhysicalObject(string name, Sprite sprite, Vector3 position)
        {
            GameObject gameObject = new GameObject(name, new Type[]
            {
            typeof(SpriteRenderer),
            typeof(AudioSourceTimeScaleBehaviour),
            typeof(Optout)
            });
            gameObject.transform.position = position;
            gameObject.layer = LayerMask.NameToLayer("Objects");
            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
            gameObject.AddComponent<BoxCollider2D>();
            gameObject.AddComponent<Rigidbody2D>();
            PhysicalBehaviour physicalBehaviour = gameObject.AddComponent<PhysicalBehaviour>();
            physicalBehaviour.Properties = ModAPI.FindPhysicalProperties("Metal");
            physicalBehaviour.SpawnSpawnParticles = false;
            gameObject.AddComponent<AudioSourceTimeScaleBehaviour>();
            physicalBehaviour.OverrideShotSounds = Array.Empty<AudioClip>();
            physicalBehaviour.OverrideImpactSounds = Array.Empty<AudioClip>();
            return gameObject;
        }
        internal static void TryCatchAction(Action tryAction, Action catchAction)
        {
            try
            {
                tryAction?.Invoke();
            }
            catch
            {
                catchAction?.Invoke();
            }
        }
        internal static void OnlyOneTimeAction(this GameObject gameObject, UnityAction Action)
        {
            if (!gameObject.GetComponent<Dont>()) Action.Invoke();
            gameObject.GetOrAddComponent<Dont>();
        }
        public static GameObject GetPerformedMod(SpawnableAsset asset, Vector3 position)
        {
            GameObject Prefab = UnityEngine.Object.Instantiate(asset.Prefab, position, Quaternion.identity);
            Prefab.AddComponent<AudioSourceTimeScaleBehaviour>();
            Prefab.name = asset.name;
            Prefab.GetOrAddComponent<SerialiseInstructions>().OriginalSpawnableAsset = asset;
            CatalogBehaviour.PerformMod(asset, Prefab);
            return Prefab;
        }
        public static T[] RemoveNulls<T>(this T[] objects) where T : class
        {
            var list = new List<T>();
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    list.Add(obj);
                }
            }
            return list.ToArray();
        }
    }

    #region Serialisation
    public class ObjectSerialisationHelper : MonoBehaviour
    {
        private bool IsSerialisableBehaviour(MonoBehaviour behaviour)
        {
            Type type = behaviour.GetType();
            if (type.GetCustomAttribute<SkipSerialisationAttribute>() == null && !notSerialisableBehaviours.Contains(type))
            {
                return true;
            }
            return false;
        }
        private void OnBeforeSerialise()
        {
            objectName = Utils.GetHierachyPath(Object.transform);
            Object.BroadcastMessage("OnBeforeSerialise", SendMessageOptions.DontRequireReceiver);
            transformPrototype = new TransformPrototype(Object.transform.localPosition, Object.transform.eulerAngles.z, Object.transform.localScale);
            List<MonoBehaviourData> list = new List<MonoBehaviourData>();
            foreach (MonoBehaviour behaviour in Object.GetComponents<MonoBehaviour>())
            {
                if (IsSerialisableBehaviour(behaviour))
                {
                    list.Add(new MonoBehaviourData(behaviour));
                }
            }
            Behaviours = list.ToArray();
        }
        private void OnAfterDeserialise()
        {
            Transform objectTransform = transform.Find(objectName);
            objectTransform.position = objectTransform.parent.TransformPoint(transformPrototype.RelativePosition);
            objectTransform.eulerAngles = new Vector3(0, 0, transform.eulerAngles.z + transformPrototype.RelativeRotation);
            objectTransform.localScale = transformPrototype.LocalScale;
            List<MonoBehaviourData> behaviourDatas = Behaviours.ToList();
            foreach (MonoBehaviour behaviour in objectTransform.GetComponents<MonoBehaviour>())
            {
                if (IsSerialisableBehaviour(behaviour))
                {
                    try
                    {
                        MonoBehaviourData data = behaviourDatas.First(b => b.Type == behaviour.GetType());
                        data.WriteToMonoBehaviour(behaviour);
                        behaviourDatas.Remove(data);
                    }
                    catch (InvalidOperationException)
                    {
                        Debug.LogWarning("Could not find data for " + behaviour.GetType().FullName);
                    }
                }
            }
            foreach (MonoBehaviourData behaviourData in behaviourDatas)
            {
                MonoBehaviour behaviour = (MonoBehaviour)objectTransform.gameObject.AddComponent(behaviourData.Type);
                behaviourData.WriteToMonoBehaviour(behaviour);
            }
            Object = objectTransform.gameObject;
        }

        [SkipSerialisation]
        public GameObject Object;
        public string objectName;
        public TransformPrototype transformPrototype;
        public MonoBehaviourData[] Behaviours;
        public LocalVariableData[] LocalVariables;
        private Type[] notSerialisableBehaviours = new Type[]
        {
            typeof(AudioSourceTimeScaleBehaviour),
            typeof(DeregisterBehaviour),
            typeof(ContextMenuOptionComponent),
            typeof(DecalControllerBehaviour),
            typeof(Optout)
        };
    }
    [Serializable]
    public struct MonoBehaviourData
    {
        public MonoBehaviourData(MonoBehaviour behaviour)
        {
            Type = behaviour.GetType();
            Fields = new Dictionary<string, object>();
            Properties = new Dictionary<string, object>();
            foreach (FieldInfo field in Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (IsSerialisableField(field))
                {
                    Fields.Add(field.Name, field.GetValue(behaviour));
                }
            }
            foreach (PropertyInfo property in Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (IsSerialisableProperty(property))
                {
                    Properties.Add(property.Name, property.GetValue(behaviour));
                }
            }
        }
        public void WriteToMonoBehaviour(MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour.GetType() != Type)
            {
                return;
            }
            foreach (FieldInfo field in Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (IsSerialisableField(field) && Fields.ContainsKey(field.Name))
                {
                    field.SetValue(monoBehaviour, Fields[field.Name]);
                }
            }
            foreach (PropertyInfo property in Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (IsSerialisableProperty(property) && Properties.ContainsKey(property.Name))
                {
                    property.SetValue(monoBehaviour, Properties[property.Name]);
                }
            }
        }
        private bool IsSerialisableField(FieldInfo field)
        {
            if (Utils.IsSerialisableType(field.FieldType) && field.GetCustomAttribute<SkipSerialisationAttribute>() == null)
            {
                return true;
            }
            return false;
        }
        private bool IsSerialisableProperty(PropertyInfo property)
        {
            if (Utils.IsSerialisableType(property.PropertyType) && property.GetCustomAttribute<SkipSerialisationAttribute>() == null && property.SetMethod != null)
            {
                return true;
            }
            return false;
        }

        public Type Type;
        public Dictionary<string, object> Fields;
        public Dictionary<string, object> Properties;
    }
    [Serializable]
    public struct LocalVariableData
    {
        public LocalVariableData(string name, object value)
        {
            Name = name;
            Value = value;
        }
        public string Name;
        public object Value;
    }
    public static class Extensions
    {
        public static bool Contains(this LocalVariableData[] array, string name)
        {
            if (array == null || array.Length == 0)
            {
                return false;
            }
            LocalVariableData variable = Array.Find(array, v => v.Name == name);
            if (variable.Name == null)
            {
                return false;
            }
            return true;
        }
    }
    #endregion
    #region AttachmentsModule
    public class AttachmentAttribute : MonoBehaviour
    {
        public string attributeName;
        [SkipSerialisation]
        public bool attached;
        public LimbClassification limbClassification;
        public Vector2 offsetPosition;
        public Vector3 offsetRotation;
        public Vector2 scale;
        public PersonBehaviour personBehaviour;
        public FixedJoint2D fixedJoint;
        public Vector2 connectedAnchor;
        private PhysicalBehaviour physicalBehaviour;
        public enum LimbClassification
        {
            NoLimb,
            Head,
            UpperBody,
            MiddleBody,
            LowerBody,
            UpperArm,
            LowerArm,
            Foot
        }
        private void Start()
        {
            physicalBehaviour = gameObject.GetComponent<PhysicalBehaviour>();
            physicalBehaviour.ContextMenuOptions.Buttons.Add(new ContextMenuButton(() => attached, "detachAttribute", "Detach " + attributeName, "Detach " + attributeName, () =>
            {
                Detach();
            }));
            AttachemntsUtils.attachmentAttributes.Add(this);
            physicalBehaviour.HoldingPositions = new Vector3[] { };
            OnInitialized();
        }
        public virtual void OnInitialized()
        {
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {

            if (!collision.gameObject.IsAlreadyAttached())
            {
                if (collision.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb))
                {
                    if (limb.GetLimbClassification() == limbClassification)
                    {
                        if (!attached)
                        {
                            personBehaviour = limb.Person;
                            Attach(collision.rigidbody);
                        }
                        else
                        {
                            SelfEffect(collision);
                        }
                    }
                }
            }
            if (attached && personBehaviour != null)
            {
                if (personBehaviour.gameObject.GetComponentsInChildren<Collider2D>().Contains(collision.collider))
                {
                    Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
                }
                var grips = personBehaviour.gameObject.GetComponentsInChildren<GripBehaviour>();
                var gripsObjects = grips.Where(grip => grip.CurrentlyHolding != null).Select(grip => grip.CurrentlyHolding.gameObject);
                if (gripsObjects.Contains(collision.gameObject))
                {
                    Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
                }
            }
        }
        private void SwitchCollision(PersonBehaviour person, bool enable = false)
        {
            foreach (var limb in personBehaviour.Limbs)
            {
                Physics2D.IgnoreCollision(limb.Collider, gameObject.GetComponent<Collider2D>(), !enable);
            }
            foreach (var attachmentAttribute in AttachemntsUtils.attachmentAttributes)
            {
                Physics2D.IgnoreCollision(attachmentAttribute.gameObject.GetComponent<Collider2D>(), gameObject.GetComponent<Collider2D>(), !enable);
            }
        }
        private void Attach(Rigidbody2D rigidbody)
        {
            SwitchCollision(personBehaviour);
            var mapScale = rigidbody.transform.root.localScale * scale * rigidbody.transform.localScale;
            gameObject.transform.position = new Vector3(rigidbody.transform.position.x + offsetPosition.x * mapScale.x, rigidbody.transform.position.y + offsetPosition.y * mapScale.y);
            var rotation = gameObject.transform.rotation;
            var rotationScale = rigidbody.transform.root.localScale.x >= 0 ? 1 : 3;
            rotation.eulerAngles = new Vector3(rigidbody.transform.rotation.eulerAngles.x + offsetRotation.x, rigidbody.transform.rotation.eulerAngles.y + offsetRotation.y, rigidbody.transform.rotation.eulerAngles.z + offsetRotation.z * rotationScale);
            gameObject.transform.rotation = rotation;
            gameObject.transform.localScale = new Vector3(mapScale.x, mapScale.y, 1);
            fixedJoint = gameObject.AddComponent<FixedJoint2D>();
            fixedJoint.connectedBody = rigidbody;
            if (connectedAnchor != new Vector2(0, 0))
            {
                fixedJoint.autoConfigureConnectedAnchor = false;
                fixedJoint.connectedAnchor = connectedAnchor/* * rigidbody.transform.root.localScale*/;
            }
            attached = true;
            var spriteRenderers = gameObject.transform.root.GetComponentsInChildren<SpriteRenderer>().ToList();
            spriteRenderers.Add(gameObject.GetComponent<SpriteRenderer>());
            foreach (var spriteRenderer in spriteRenderers)
            {
                var rigidBodyRenderer = rigidbody.gameObject.GetComponent<SpriteRenderer>();
                spriteRenderer.sortingLayerName = rigidBodyRenderer.sortingLayerName;
                spriteRenderer.sortingOrder = rigidBodyRenderer.sortingOrder + 1;
            }
            if (gameObject.TryGetComponent<SortingGroup>(out SortingGroup sortingGroup))
            {
                var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                sortingGroup.sortingLayerName = spriteRenderer.sortingLayerName;
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
            OnAttach();
        }
        public virtual void SelfEffect(Collision2D collision2D)
        {
        }
        public virtual void OnAttach()
        {
        }
        public virtual void OnDetach()
        {
        }
        public void Detach()
        {
            OnDetach();
            if (fixedJoint != null)
            {
                UnityEngine.Object.Destroy(fixedJoint);
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    if (personBehaviour != null)
                    {
                        SwitchCollision(personBehaviour, true);
                    }
                    personBehaviour = null;
                    attached = false;
                });
            }
            fixedJoint = null;
        }
        public virtual void OnDestroy()
        {
            AttachemntsUtils.attachmentAttributes.Remove(this);
            Detach();
        }
    }
    public static class AttachemntsUtils
    {
        public static List<AttachmentAttribute> attachmentAttributes = new List<AttachmentAttribute>();
        public static bool IsAlreadyAttached(this GameObject attachment)
        {
            bool attached = false;
            foreach (var attachmentAttribute in attachmentAttributes)
            {
                if (attachmentAttribute.fixedJoint != null)
                {
                    if (attachmentAttribute.fixedJoint.connectedBody.gameObject == attachment)
                    {
                        attached = true;
                    }
                }
            }
            return attached;
        }
        public static List<AttachmentAttribute> GetAttachmentAttributes(PersonBehaviour personBehaviour)
        {
            return attachmentAttributes.Where(a => a != null).Where(a => a.personBehaviour != null && a.attached).Where(a => a.personBehaviour == personBehaviour).ToList();
        }
        public static AttachmentAttribute.LimbClassification GetLimbClassification(this LimbBehaviour limbBehaviour)
        {
            var limbName = limbBehaviour.name;
            if (limbName.Contains("Head"))
            {
                return AttachmentAttribute.LimbClassification.Head;
            }
            else if (limbName.Contains("UpperBody"))
            {
                return AttachmentAttribute.LimbClassification.UpperBody;
            }
            else if (limbName.Contains("MiddleBody"))
            {
                return AttachmentAttribute.LimbClassification.MiddleBody;
            }
            else if (limbName.Contains("LowerBody"))
            {
                return AttachmentAttribute.LimbClassification.LowerBody;
            }
            else if (limbName.Contains("UpperArm"))
            {
                return AttachmentAttribute.LimbClassification.UpperArm;
            }
            else if (limbName.Contains("LowerArm"))
            {
                return AttachmentAttribute.LimbClassification.LowerArm;
            }
            else if (limbName.Contains("Foot"))
            {
                return AttachmentAttribute.LimbClassification.Foot;
            }
            else
            {
                return AttachmentAttribute.LimbClassification.NoLimb;
            }
        }
    }
    #endregion
    #region RegrowthModule
    // Не пытайтесь понять что здесь происходит, когда вы читаете, я скорее всего сам уже не знаю что за пиздец здесь происходит. Но если вы решили всё таки окунуться в тазик говна, то я оставил некоторые наскальные надписи.
    // todo сделать очищение лимба от посторонних жидкостей
    public class RegrowthModule : MonoBehaviour
    {
        public PersonBehaviour PersonBehaviour;
        public LimbInformation[] LimbInformations;
        public LimbBehaviour[] DestroyedLimbs
        {
            get
            {
                return callBacks.Where(callback => (!callback.LimbBehaviour.NodeBehaviour.IsConnectedToRoot/* || callback.LimbBehaviour.IsDismembered */|| !callback.LimbBehaviour.gameObject.activeSelf) && !callback.LimbBehaviour.NodeBehaviour.IsRoot).Select(callback => callback.LimbBehaviour).ToArray();
            }
        }
        public List<RegrowthModuleCallBack> callBacks = new List<RegrowthModuleCallBack>();
        public bool Debugging = true;
        public Controller controller;
        public bool Active = true;
        private void Start()
        {
            PersonBehaviour = gameObject.GetComponent<PersonBehaviour>();
            if (LimbInformations == null)
            {
                CollectInformation();
            }
            if (callBacks.Count == 0)
            {
                InitializeCallBack();
            }
        }
        private void InitializeCallBack()
        {
            foreach (var limb in PersonBehaviour.Limbs)
            {
                var callBack = limb.gameObject.GetOrAddComponent<RegrowthModuleCallBack>();
                callBack.RegrowthModule = this;
                callBacks.Add(callBack);
            }
        }
        private void CollectInformation() => StartCoroutine(CollectInformationCoroutine());
        #region RegrowthLogic
        private void ReabilityLimb(LimbBehaviour limbBehaviour)
        {
            limbBehaviour.gameObject.SetActive(true); // убеждаемся в том что лимба включена, в случае краша она отключается
            foreach (var colliider in limbBehaviour.PhysicalBehaviour.colliders) // врубаем коллайдеры если они выключены
            {
                colliider.enabled = true;
            }
            limbBehaviour.SkinMaterialHandler.renderer.enabled = true; // врубаем рендерер
            limbBehaviour.PhysicalBehaviour.isDisintegrated = false;
            limbBehaviour.PhysicalBehaviour.rigidbody.simulated = true;
            if (limbBehaviour.gameObject.TryGetComponent(out FreezeBehaviour freezeBehaviour))
            {
                freezeBehaviour.Destroy();
            }
            limbBehaviour.PhysicalBehaviour.rigidbody.bodyType = RigidbodyType2D.Dynamic;
            ClearOnCollisionBuffer(limbBehaviour.PhysicalBehaviour);
            if (controller != null)
            {
                if (controller.DestroyWires)
                {
                    DestroyWires(limbBehaviour);
                }
                else
                {
                    BackWires(limbBehaviour);
                }
            }
            else
            {
                DestroyWires(limbBehaviour);
            }
        }
        public static void ClearOnCollisionBuffer(PhysicalBehaviour physicalBehaviour)
        {
            var type = Type.GetType("PhysicalBehaviour+ColliderBoolPair, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            var constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(Collider2D) });
            var array = physicalBehaviour.GetField<PhysicalBehaviour, object[]>("onCollisionStayBuffer");
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = null;
            }
            physicalBehaviour.SetField("onCollisionStayBuffer", array);
        }
        public void ReabilityJoint(LimbBehaviour limbBehaviour)
        {
            if (Debugging)
                Debug.Log($"[RegrowthModule] {limbBehaviour.name} request reability joint");
            var limbInfo = GetLimbInformation(limbBehaviour);
            var jointInfo = limbInfo.hingeJointInformation;
            LimbBehaviour probablyGoodLimb = null;
            // пробуем найти конечность к которой присобачена наша и возьмём её ротацию и позицию если до этого нихуя не было
            try
            {
                probablyGoodLimb = GetLimbFromPath(jointInfo.attachedHingePaths.Where(path => GetLimbFromPath(path).gameObject.activeSelf)?.First());
            }
            catch { }
            if (probablyGoodLimb != null) // чё это вообще такое, к примеру для upperBody это Head, у upperArm это upperBody. Но эта штука также может быть null. К примеру у middleBody это будет upperBody, но сам middleBody не имеет джоинта, соответственно для lowerBody это будет null, спасибо zooi за лучшую структуру. Поэтому если это дерьмо будет null, то конечность должна самостоятельно создать себе джоинт и восстановить логическую цепочку. Хоть на логику здесь все хуй положили, но в общих чертах она присутствует. Мне похуй что этот комментарий такой длинный.
            {
                limbBehaviour.transform.position = probablyGoodLimb.transform.position;
                limbBehaviour.PhysicalBehaviour.rigidbody.rotation = probablyGoodLimb.PhysicalBehaviour.rigidbody.rotation; // используем Rigidbody2D.rotation, потому что HingeJoint2D при установке upperBody не использует позицию Transform.rotation, он использует Rigidbody2D.position для расчёта угла между объектом и connectedBody. Если мы используем transform.rotation, то будет отклонение в несколько градусов. Это ещё один из длинных комментариев.
                if (GetEmptyJoint(probablyGoodLimb) != null)
                {
                    if (Debugging)
                        Debug.Log($"[RegrowthModule] {limbBehaviour.name} find PGL: {probablyGoodLimb.name}");
                    var probablyGoodLimbJoint = GetEmptyJoint(probablyGoodLimb);
                    var pInfo = GetLimbInformation(probablyGoodLimb);
                    var probablyGoodLimbJointInfo = pInfo.hingeJointInformation;
                    probablyGoodLimbJoint.autoConfigureConnectedAnchor = false;
                    probablyGoodLimbJoint.anchor = probablyGoodLimbJointInfo.anchor;
                    probablyGoodLimbJoint.connectedAnchor = probablyGoodLimbJointInfo.connectedAnchor;
                    probablyGoodLimb.BreakingThreshold = pInfo.breakingThresold;
                    probablyGoodLimbJoint.breakForce = probablyGoodLimbJointInfo.breakForce;
                    probablyGoodLimbJoint.breakTorque = probablyGoodLimbJointInfo.breakTorque;
                    probablyGoodLimbJoint.limits = probablyGoodLimbJointInfo.jointAngleLimits;
                    probablyGoodLimbJoint.connectedBody = limbBehaviour.PhysicalBehaviour.rigidbody;
                    probablyGoodLimbJoint.useLimits = probablyGoodLimbJointInfo.useLimits;
                    probablyGoodLimbJoint.useMotor = true;
                    probablyGoodLimb.Joint = probablyGoodLimbJoint;
                    probablyGoodLimb.HasJoint = true;
                    probablyGoodLimb.IsDismembered = false;
                    probablyGoodLimb.SendMessage("SetupJoint");
                    if (probablyGoodLimb.gameObject.TryGetComponent(out GoreStringBehaviour goreStringBehaviour))
                    {
                        goreStringBehaviour.DestroyJoint();
                    }
                    Utility.InvokeAfterDelay(2f, () => probablyGoodLimb.Joint.autoConfigureConnectedAnchor = true);
                }
            }
            else if (jointInfo.hasInformation)
            {
                var connectedBody = GetLimbFromPath(jointInfo.connectedBodyPath);
                if (connectedBody.gameObject.activeSelf)
                {
                    limbBehaviour.transform.position = connectedBody.transform.position;
                    limbBehaviour.PhysicalBehaviour.rigidbody.rotation = connectedBody.PhysicalBehaviour.rigidbody.rotation;  // используем Rigidbody2D.rotation, потому что HingeJoint2D при установке upperBody не использует позицию Transform.rotation, он использует Rigidbody2D.position для расчёта угла между объектом и connectedBody. Если мы используем transform.rotation, то будет отклонение в несколько градусов. Это ещё один из длинных комментариев.
                    var joint = GetEmptyJoint(limbBehaviour);
                    if (Debugging)
                        Debug.Log($"[RegrowthModule] {limbBehaviour.name} create self joint: {connectedBody.name}");
                    joint.autoConfigureConnectedAnchor = false;
                    joint.anchor = jointInfo.anchor;
                    joint.connectedAnchor = jointInfo.connectedAnchor;
                    limbBehaviour.BreakingThreshold = limbInfo.breakingThresold;
                    joint.breakForce = jointInfo.breakForce;
                    joint.breakTorque = jointInfo.breakTorque;
                    joint.limits = jointInfo.jointAngleLimits;
                    joint.connectedBody = connectedBody.PhysicalBehaviour.rigidbody;
                    joint.useLimits = jointInfo.useLimits;
                    joint.useMotor = true;
                    limbBehaviour.Joint = joint;
                    limbBehaviour.HasJoint = true;
                    limbBehaviour.IsDismembered = false;
                    limbBehaviour.SendMessage("SetupJoint");
                    Utility.InvokeAfterDelay(2f, () => limbBehaviour.Joint.autoConfigureConnectedAnchor = true);
                }
            }
            PersonBehaviour.gameObject.NoChildCollide(); // вырубаем коллизии между джоинтами
        }
        public void BackWires(LimbBehaviour limbBehaviour)
        {
            foreach (var wire in limbBehaviour.gameObject.GetComponentsInChildren<LineRenderer>())
            {
                if (wire.gameObject.name == "Wire")
                {
                    wire.enabled = true;
                }
            }
        }
        public void DestroyWires(LimbBehaviour limbBehaviour)
        {
            foreach (var component in limbBehaviour.gameObject.GetComponents<Component>())
            {
                try
                {
                    var hover = (Hover)component;
                    if (hover != null)
                    {
                        hover.Destroy();
                    }
                }
                catch { }
            }
        }
        public void ConnectToLimbSystem(LimbBehaviour limbBehaviour)
        {
            var limbInfo = GetLimbInformation(limbBehaviour);
            limbBehaviour.ConnectedLimbs = new List<LimbBehaviour>();
            // LimbBehaviour.ConnectedLimbs
            foreach (var connectedLimbPath in limbInfo.connectedLimbsPath) // убедитесь что лимбы не уничтожены
            {
                var connectedLimb = GetLimbFromPath(connectedLimbPath);
                connectedLimb.IsDismembered = false;
                limbBehaviour.ConnectedLimbs.Add(connectedLimb);
                connectedLimb.ConnectedLimbs.Add(limbBehaviour);
            }
            // limbBehaviour.SkinMaterialHandler.adjacentLimbs
            limbBehaviour.SkinMaterialHandler.adjacentLimbs = new SkinMaterialHandler[limbInfo.adjacentLimbs.Length];
            for (int i = 0; i < limbInfo.adjacentLimbs.Length; i++)
            {
                limbBehaviour.SkinMaterialHandler.adjacentLimbs[i] = GetLimbFromPath(limbInfo.adjacentLimbs[i]).SkinMaterialHandler;
            }
            // limbBehaviour.CirculationBehaviour.PushesTo
            limbBehaviour.CirculationBehaviour.PushesTo = new CirculationBehaviour[limbInfo.pushToLimbsPath.Length];
            for (int i = 0; i < limbInfo.pushToLimbsPath.Length; i++)
            {
                limbBehaviour.CirculationBehaviour.PushesTo[i] = GetLimbFromPath(limbInfo.pushToLimbsPath[i]).CirculationBehaviour;
                if (limbBehaviour.CirculationBehaviour.PushesTo[i].gameObject.activeSelf)
                {
                    limbBehaviour.CirculationBehaviour.PushesTo[i].IsDisconnected = false;
                }
            }
            if (limbInfo.sourcePath != "")
            {
                limbBehaviour.CirculationBehaviour.Source = GetLimbFromPath(limbInfo.sourcePath).CirculationBehaviour;
            }
            limbBehaviour.CirculationBehaviour.IsPump = limbBehaviour.CirculationBehaviour.WasInitiallyPumping;
            limbBehaviour.CirculationBehaviour.IsDisconnected = false;
            // NodeBehaviour
            var nodeInformation = limbInfo.nodeInformation;
            limbBehaviour.NodeBehaviour.Connections = new ConnectedNodeBehaviour[nodeInformation.connectionsTransformPaths.Length];
            for (int i = 0; i < nodeInformation.connectionsTransformPaths.Length; i++)
            {
                limbBehaviour.NodeBehaviour.Connections[i] = GetLimbFromPath(nodeInformation.connectionsTransformPaths[i]).NodeBehaviour;
            }
            foreach (var connectedNodePath in nodeInformation.indexInConnectetions)
            {
                var connectedNode = GetLimbFromPath(connectedNodePath.Key);
                connectedNode.NodeBehaviour.Connections[connectedNodePath.Value] = limbBehaviour.NodeBehaviour;
            }
            limbBehaviour.NodeBehaviour.Value = nodeInformation.value;
            limbBehaviour.NodeBehaviour.IsRoot = nodeInformation.isRoot;
            limbBehaviour.NodeBehaviour.RootPropagation();
            limbBehaviour.IsDismembered = false;
        }
        public void RegrowthNearestLimb()
        {
            if (DestroyedLimbs.Length > 0)
                RegrowthLimb(DestroyedLimbs.OrderBy(limb => limb.DistanceToBrain).First(), null, false, 1);
        }
        public void RegrowthAll()
        {
            StartCoroutine(RegrowthAllCoroutine());
        }
        // limbBehaviour - кончность которую надо отрастить
        // requester - конечность которая просит другую конечность отрастится чтобы отращивание limbBehaviour был возможен, короче пропагация, если вы вызываете этот метод - оставляете этот параметр null
        // onlyNeeded - если true то отрастит только нужные для отращивания limbBehaviour, к примеру вы отрываете полностью руку, сначало lowerArm попросит upperArm отрастится и на этом всё закончится
        // а если к примеру от upperBody был оторван middleBody то пропагация не пойдёт к нему, но если onlyNeeded на false, но пропагация проверит соседние конечности если они дальше по distanceBrain
        // limitRegrowthLimbs - ограничение по отращиванию конечностей
        // denyRoot - если вам каким то хуем надо голову отрастить - ставьте на false
        public void RegrowthLimb(LimbBehaviour limbBehaviour, LimbBehaviour requester = null, bool onlyNeeded = true, int limitRegrowthLimbs = int.MaxValue, bool denyRoot = true)
        {
            if (!Active)
            {
                if (Debugging)
                    Debug.Log("[RegrowthModule] Regrowth stopped, script don't active");
                return;
            }
            if (!PersonBehaviour.IsAlive())
            {
                if (controller != null)
                {
                    if (controller.DontRegrowthDead)
                    {
                        if (Debugging)
                            Debug.Log("[RegrowthModule] Regrowth stopped, omg he dead!");
                        return;
                    }
                }
                else
                {
                    if (Debugging)
                        Debug.Log("[RegrowthModule] Regrowth stopped, omg he dead!");
                    return;
                }
            }
            if (!PersonBehaviour.Limbs[0].gameObject.activeSelf && denyRoot)
            {
                if (controller != null)
                {
                    if (controller.DontRegrowthWithoutRoot)
                    {
                        if (Debugging)
                            Debug.Log("[RegrowthModule] Regrowth stopped, root is destroyed");
                        return;
                    }
                }
                else
                {
                    if (Debugging)
                        Debug.Log("[RegrowthModule] Regrowth stopped, root is destroyed");
                    return;
                }
            }
            if (limbBehaviour.NodeBehaviour.IsRoot && denyRoot)
            {
                if (Debugging)
                    Debug.Log($"[RegrowthModule] {limbBehaviour.name} Denial of regrowth, limb is root.");
                return;
            }
            if (limitRegrowthLimbs <= 0)
            {
                if (Debugging)
                    Debug.Log($"[RegrowthModule] {limbBehaviour.name} Denial of regrowth, over the limit.");
                return;
            }

            bool fakeLimbsCreated = false;
            PersonBehaviour fakePerson = null;
            if (requester == null)
            {
                if (controller != null)
                {
                    if (controller.createFakeLimbs)
                    {
                        if (Debugging)
                            Debug.Log($"[RegrowthModule] {limbBehaviour.name} request create fakeLimbs");
                        if (PersonBehaviour.Limbs.Where(l => l.gameObject.activeSelf && !l.NodeBehaviour.IsConnectedToRoot).ToArray().Length > 0) // проверяем нужно ли копировать
                        {
                            fakeLimbsCreated = true;
                            var fakePersonSerialized = new SerializedObjects();
                            fakePersonSerialized.Objects = ObjectStateConverter.Convert(PersonBehaviour.Limbs.Select(l => l.gameObject).ToList(), PersonBehaviour.gameObject.transform.position).ToList();
                            GameObject[] fakePersonObjects = ObjectStateConverter.Convert(fakePersonSerialized.Objects, PersonBehaviour.gameObject.transform.position);
                            fakePerson = fakePersonObjects[0].gameObject.GetComponent<PersonBehaviour>();
                            foreach (var fakeLimb in fakePerson.Limbs)
                            {
                                fakeLimb.PhysicalBehaviour.SpawnSpawnParticles = false;
                            }
                            fakePerson.Limbs[0].PhysicalBehaviour.Disintegrate();
                            if (fakePerson.gameObject.TryGetComponent(out RegrowthModule.Controller controller))
                            {
                                controller.Destroy();
                            }
                            if (fakePerson.gameObject.TryGetComponent<RegrowthModule>(out RegrowthModule regrowthModule))
                            {
                                regrowthModule.Destroy();
                            }
                            var fakePersonColliders = fakePerson.GetComponentsInChildren<Collider2D>();
                            var personColliders = PersonBehaviour.gameObject.GetComponentsInChildren<Collider2D>();
                            foreach (var fakePersonCollider in fakePersonColliders)
                            {
                                foreach (var personCollider in personColliders)
                                {
                                    Physics2D.IgnoreCollision(fakePersonCollider, personCollider, true);
                                }
                            }
                        }
                    }
                }
                foreach (var connectedLimb in limbBehaviour.ConnectedLimbs.Where(l => l.DistanceToBrain > limbBehaviour.DistanceToBrain && l.NodeBehaviour.IsConnectedToRoot))
                {
                    connectedLimb.IsDismembered = true;
                }
                foreach (var limbToCrush in DestroyedLimbs)
                {
                    var disCounter = gameObject.GetComponent<DisintegrationCounterBehaviour>();
                    if (controller != null)
                    {
                        if (controller.DisintegrationCounterSelfControl)
                        {
                            disCounter.DisintegrationCount = DestroyedLimbs.Length - 1; // великий компонент zooi, если 14 раз отреабилиторвать конечность, весь чел исчезнет нахуй
                        }
                    }
                    else
                    {
                        disCounter.DisintegrationCount = DestroyedLimbs.Length - 1;
                    }
                    if (controller != null)
                    {
                        switch (controller.destroyType)
                        {
                            case Controller.LimbDestroyType.Crush:
                                limbToCrush.Crush();
                                break;
                            case Controller.LimbDestroyType.Disintegrate:
                                limbToCrush.PhysicalBehaviour.Disintegrate();
                                break;
                            default:
                                limbToCrush.Crush();
                                break;
                        }
                    }
                    else
                    {
                        limbToCrush.Crush();
                    }
                }
            }
            var connectedLimbsNeedRegrowth = new List<LimbBehaviour>(); // конечности которые требуют отращивания перед тем как отрастить конечность которую вы передали в параметр
            var limbInfo = GetLimbInformation(limbBehaviour);
            foreach (var connectedLimbPath in limbInfo.connectedLimbsPath)
            {
                var connectedLimb = GetLimbFromPath(connectedLimbPath);
                if ((!connectedLimb.NodeBehaviour.IsConnectedToRoot || connectedLimb.IsDismembered || !connectedLimb.NodeBehaviour.IsRoot) && connectedLimb != requester)
                {
                    if (onlyNeeded)
                    {
                        if (connectedLimb.DistanceToBrain < limbBehaviour.DistanceToBrain)
                        {
                            connectedLimbsNeedRegrowth.Add(connectedLimb);
                        }
                    }
                    else
                    {
                        connectedLimbsNeedRegrowth.Add(connectedLimb);
                    }
                }
            }
            var oldDl = DestroyedLimbs;
            ReabilityLimb(limbBehaviour);
            ConnectToLimbSystem(limbBehaviour);
            if (fakeLimbsCreated)
            {
                foreach (var fakeLimb in fakePerson.Limbs)
                {
                    var originalLimb = PersonBehaviour.Limbs.Where(l => l.name == fakeLimb.name);
                    if (originalLimb.ToArray().Length > 0)
                    {
                        if (originalLimb.First().NodeBehaviour.IsConnectedToRoot)
                        {
                            fakeLimb.PhysicalBehaviour.Disintegrate();
                        }
                    }
                }
                controller.AfterCreateFakeLimbs(fakePerson);
            }
            foreach (var connectedLimbToRegrowth in connectedLimbsNeedRegrowth)
            {
                if (Debugging)
                    Debug.Log($"[RegrowthModule] {limbBehaviour.name} request Regrowth Limb: {connectedLimbToRegrowth.name}");
                RegrowthLimb(connectedLimbToRegrowth, limbBehaviour, onlyNeeded, limitRegrowthLimbs - 1);
            }
            ReabilityJoint(limbBehaviour);
            if (limbBehaviour.gameObject.TryGetComponent(out GoreStringBehaviour goreStringBehaviour))
            {
                if (Debugging)
                    Debug.Log($"[RegrowthModule] {limbBehaviour.name} destroyed gore strings");
                goreStringBehaviour.DestroyJoint();
            }
            if (limbBehaviour.gameObject.TryGetComponent(out GripBehaviour gripBehaviour))
            {
                gripBehaviour.DropObject();
            }
            if (limbBehaviour.transform.root.gameObject.TryGetComponent<AudioSourceTimeScaleBehaviour>(out AudioSourceTimeScaleBehaviour audioSourceTimeScaleBehaviour))
            {
                var startAudioSource = typeof(AudioSourceTimeScaleBehaviour).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                startAudioSource.Invoke(audioSourceTimeScaleBehaviour, new object[] { });
            }
            if (controller != null)
            {
                controller.OnRegrowthLimb(limbBehaviour);
            }
            if (Debugging)
                Debug.Log($"[RegrowthModule] {limbBehaviour.name} end Regrowth Proccess");
        }
        #endregion
        #region Coroutines
        private IEnumerator CollectInformationCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            if (Debugging)
                Debug.Log($"[RegrowthModule] Start Collect Limbs Information");
            LimbInformations = new LimbInformation[PersonBehaviour.Limbs.Length];
            for (int i = 0; i < PersonBehaviour.Limbs.Length; i++)
            {
                LimbInformations[i] = new LimbInformation(PersonBehaviour.Limbs[i]);
            }
            if (Debugging)
                Debug.Log($"[RegrowthModule] Limbs Collected Information");
        }
        private IEnumerator RepositingLimbsCoroutine()
        {
            for (int i = 0; i < PersonBehaviour.Limbs.Length; i++)
            {
                yield return new WaitForEndOfFrame();
                foreach (var limbInfo in LimbInformations.OrderBy(a => a.distanceBrain))
                {
                    var limb = GetLimbFromPath(limbInfo.transformPath);
                    var jointInfo = limbInfo.hingeJointInformation;
                    if (jointInfo.hasInformation && limb.HasJoint && limb.Joint?.connectedBody != null)
                    {
                        var connectedBody = GetLimbFromPath(jointInfo.connectedBodyPath);
                        Vector2 Displacement = connectedBody.transform.TransformPoint(jointInfo.connectedAnchor) - connectedBody.gameObject.transform.TransformPoint(limb.Joint.connectedAnchor);
                        limb.transform.position = (Vector2)limb.transform.position + Displacement;
                    }
                }
            }
        }
        private HingeJoint2D GetEmptyJoint(LimbBehaviour limbBehaviour)
        {
            if (limbBehaviour.Joint != null)
            {
                return limbBehaviour.Joint;
            }
            else
            {
                var limbInfo = GetLimbInformation(limbBehaviour);
                var jointInfo = limbInfo.hingeJointInformation;
                if (jointInfo.hasInformation)
                {
                    limbBehaviour.Joint = limbBehaviour.gameObject.AddComponent<HingeJoint2D>();
                    limbBehaviour.HasJoint = true;
                    return limbBehaviour.Joint;
                }
                else
                {
                    return null;
                }
            }
        }
        private IEnumerator RegrowthAllCoroutine()
        {
            int countDestroyedLimbs = DestroyedLimbs.Length;
            for (int i = 0; i < countDestroyedLimbs; i++)
            {
                yield return new WaitForEndOfFrame();
                RegrowthNearestLimb();
            }
        }
        private IEnumerator RepositingLimbCoroutine(LimbBehaviour limbBehaviour)
        {
            yield return new WaitForEndOfFrame();
            var limbInfo = GetLimbInformation(limbBehaviour);
            var jointInfo = limbInfo.hingeJointInformation;
            if (jointInfo.hasInformation && limbBehaviour.HasJoint && !limbInfo.nodeInformation.isRoot)
            {
                var connectedBody = GetLimbFromPath(jointInfo.connectedBodyPath);
                Vector2 Displacement = connectedBody.transform.TransformPoint(jointInfo.connectedAnchor) - connectedBody.gameObject.transform.TransformPoint(limbBehaviour.Joint.connectedAnchor);
                limbBehaviour.transform.position = (Vector2)limbBehaviour.transform.position + Displacement;
            }
        }

        #endregion
        #region CallBack
        public Action<LimbBehaviour> OnLimbDestroyed;
        public class RegrowthModuleCallBack : MonoBehaviour
        {
            public RegrowthModule RegrowthModule;
            public LimbBehaviour LimbBehaviour;
            private void Start()
            {
                LimbBehaviour = gameObject.GetComponent<LimbBehaviour>();
            }
        }
        #endregion
        #region Other
        private LimbInformation GetLimbInformation(LimbBehaviour limbBehaviour)
        {
            return LimbInformations.Where(limbInfo => limbInfo.transformPath == Utility.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.transform)).First();
        }
        private LimbBehaviour GetLimbFromPath(string path)
        {
            return PersonBehaviour.Limbs.Where(limb => Utility.GetHierarchyPath(limb.transform.root, limb.transform) == path).First();
        }
        public void RepositingLimbs()
        {
            StartCoroutine(RepositingLimbsCoroutine());
        }
        public void RepositingLimb(LimbBehaviour limbBehaviour)
        {
            StartCoroutine(RepositingLimbCoroutine(limbBehaviour));
        }
        #endregion
        #region Controllers
        [RequireComponent(typeof(RegrowthModule))]
        [DisallowMultipleComponent]
        public abstract class Controller : MonoBehaviour
        {
            public enum ModeRegrowth
            {
                Active,
                Passive
            }
            public enum LimbDestroyType
            {
                Crush,
                Disintegrate
            }
            public virtual ModeRegrowth mode => ModeRegrowth.Active;
            internal virtual float passiveTimeUpdate => 0.01f;
            public virtual bool createFakeLimbs => false;
            public virtual LimbDestroyType destroyType => LimbDestroyType.Crush;
            public RegrowthModule regrowthModule => m_regrowthModule;
            public virtual bool DontRegrowthWithoutRoot => true;
            public virtual bool DisintegrationCounterSelfControl => true;
            public virtual bool DontRegrowthDead => true;
            public virtual bool DestroyWires => true;
            protected RegrowthModule m_regrowthModule;
            protected void Awake()
            {
                m_regrowthModule = gameObject.GetComponent<RegrowthModule>();
            }
            protected void Start()
            {
                StartCoroutine(PassiveHandler());
                AfterStart();
            }
            public virtual void AfterStart() { }
            private IEnumerator PassiveHandler()
            {
                yield return new WaitForSeconds(passiveTimeUpdate);
                if (mode == ModeRegrowth.Passive)
                {
                    OnPassiveUpdate();
                }
                StartCoroutine(PassiveHandler());
            }
            public virtual void OnPassiveUpdate()
            {
            }
            public virtual void AfterCreateFakeLimbs(PersonBehaviour fakePerson)
            {
            }
            public virtual void OnRegrowthLimb(LimbBehaviour limbBehaviour)
            {
            }
        }
        public class StandardActiveController : Controller
        {
            public override bool createFakeLimbs => true;
            public override LimbDestroyType destroyType => LimbDestroyType.Disintegrate;
            public override ModeRegrowth mode => ModeRegrowth.Active;
        }
        public class StandardPassiveController : Controller
        {
            public override bool createFakeLimbs => true;
            public override LimbDestroyType destroyType => LimbDestroyType.Disintegrate;
            public override ModeRegrowth mode => ModeRegrowth.Passive;
            internal override float passiveTimeUpdate => PassiveTimeUpdate;
            public virtual float PassiveTimeUpdate => 0.5f;
            public virtual float StepTimeAcidProgress => 0.005f;
            public virtual float StepAcidProgress => 0.01f;
            private Color emptyColor = new Color(0, 0, 0, 0);
            public List<LimbBehaviour> inRegrowth = new List<LimbBehaviour>();
            public override void OnPassiveUpdate()
            {
                Debug.Log("OnPassiveUpdate");
                if (inRegrowth.Count == 0)
                {
                    regrowthModule.RegrowthNearestLimb();
                }
            }
            public override void OnRegrowthLimb(LimbBehaviour limbBehaviour)
            {
                StartCoroutine(RegrowthEffect(limbBehaviour));
            }
            public IEnumerator RegrowthEffect(LimbBehaviour limbBehaviour)
            {
                inRegrowth.Add(limbBehaviour);
                foreach (var limb in regrowthModule.PersonBehaviour.Limbs.Where(l => !regrowthModule.DestroyedLimbs.Contains(l)))
                {
                    GeneralHealing(limb);
                }
                var originalTexture = limbBehaviour.SkinMaterialHandler.renderer.material.GetTexture("_BoneTex");
                Texture2D emptyTexture = new Texture2D(originalTexture.width, originalTexture.height);
                for (int i = 0; i < originalTexture.mipmapCount; i++)
                {
                    int pixelsCount = emptyTexture.GetPixels32(i).Length;
                    List<Color> colors = new List<Color>();
                    for (int w = 0; w < pixelsCount; w++)
                    {
                        colors.Add(emptyColor);
                    }
                    emptyTexture.SetPixels(colors.ToArray(), i);
                }
                emptyTexture.Apply();
                limbBehaviour.SkinMaterialHandler.ClearAllDamage();
                limbBehaviour.SkinMaterialHandler.renderer.material.SetTexture("_BoneTex", emptyTexture);
                limbBehaviour.SkinMaterialHandler.renderer.material.SetFloat("_AcidProgress", 1);
                for (float i = 0; i < 1; i += StepAcidProgress)
                {
                    yield return new WaitForSeconds(StepTimeAcidProgress);
                    limbBehaviour.SkinMaterialHandler.renderer.material.SetFloat("_AcidProgress", 1 - i);
                }
                limbBehaviour.SkinMaterialHandler.renderer.material.SetTexture("_BoneTex", originalTexture);
                inRegrowth.Remove(limbBehaviour);
                foreach (var limb in regrowthModule.PersonBehaviour.Limbs.Where(l => !regrowthModule.DestroyedLimbs.Contains(l)))
                {
                    GeneralHealing(limb);
                }
            }
            private void GeneralHealing(LimbBehaviour limb)
            {
                limb.HealBone();
                limb.Health = limb.InitialHealth;
                limb.Numbness = 0f;
                limb.CirculationBehaviour.HealBleeding();
                limb.CirculationBehaviour.IsPump = limb.CirculationBehaviour.WasInitiallyPumping;
                limb.CirculationBehaviour.BloodFlow = 1f;
                limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), Mathf.Max(0f, 1f - limb.CirculationBehaviour.GetAmount(limb.GetOriginalBloodType())));
                limb.BruiseCount = 0;
                limb.CirculationBehaviour.GunshotWoundCount = 0;
                limb.CirculationBehaviour.StabWoundCount = 0;
                if (limb.RoughClassification == LimbBehaviour.BodyPart.Head)
                {
                    limb.Person.Consciousness = 1f;
                    limb.Person.ShockLevel = 0f;
                    limb.Person.PainLevel = 0f;
                    limb.Person.OxygenLevel = 1f;
                    limb.Person.AdrenalineLevel = 1f;
                }
                limb.Person.Braindead = false;
                limb.Person.BrainDamaged = false;
                limb.Person.BrainDamagedTime = 0f;
                limb.Person.SeizureTime = 0f;
                limb.LungsPunctured = false;
            }
        }
        public class StandardPassiveDeadpool : StandardPassiveController
        {
            public override bool DontRegrowthWithoutRoot => false;
            public override bool DisintegrationCounterSelfControl => false;
            public DisintegrationCounterBehaviour disintegrationCounter;
            public override bool createFakeLimbs => false;
            public override bool DontRegrowthDead => false;
            public override LimbDestroyType destroyType => LimbDestroyType.Crush;
            public override void AfterStart()
            {
                disintegrationCounter = regrowthModule.PersonBehaviour.gameObject.GetComponent<DisintegrationCounterBehaviour>();
                disintegrationCounter.DisintegrationCount = int.MaxValue;
            }
            public override void OnPassiveUpdate()
            {
                if (inRegrowth.Count == 0)
                {
                    if (!regrowthModule.PersonBehaviour.Limbs[0].gameObject.activeSelf)
                    {
                        disintegrationCounter.DisintegrationCount = int.MaxValue;
                        regrowthModule.RegrowthLimb(regrowthModule.PersonBehaviour.Limbs[0], null, false, 2, false);
                    }
                    else
                    {
                        regrowthModule.RegrowthNearestLimb();
                    }
                }
            }
        }
        #endregion
    }
    public struct LimbInformation
    {
        public LimbInformation(LimbBehaviour limbBehaviour)
        {
            name = limbBehaviour.name;
            transformPath = Utility.GetHierarchyPath(limbBehaviour.Person.transform, limbBehaviour.transform);
            if (limbBehaviour.transform.parent != null)
            {
                parentTransformPath = Utility.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.transform.parent);
            }
            else
            {
                parentTransformPath = "";
            }
            connectedLimbsPath = new string[limbBehaviour.ConnectedLimbs.Count];
            for (int i = 0; i < limbBehaviour.ConnectedLimbs.Count; i++)
            {
                connectedLimbsPath[i] = Utility.GetHierarchyPath(limbBehaviour.ConnectedLimbs[i].transform.root, limbBehaviour.ConnectedLimbs[i].transform);
            }
            hingeJointInformation = new HingeJointInformation(limbBehaviour);
            orderInPerson = limbBehaviour.Person.Limbs.ToList().IndexOf(limbBehaviour);
            pushToLimbsPath = new string[limbBehaviour.CirculationBehaviour.PushesTo.Length];
            for (int i = 0; i < limbBehaviour.CirculationBehaviour.PushesTo.Length; i++)
            {
                pushToLimbsPath[i] = Utility.GetHierarchyPath(limbBehaviour.CirculationBehaviour.PushesTo[i].transform.root, limbBehaviour.CirculationBehaviour.PushesTo[i].transform);
            }

            if (limbBehaviour.CirculationBehaviour.Source != null)
            {
                sourcePath = Utility.GetHierarchyPath(limbBehaviour.CirculationBehaviour.Source.transform.root, limbBehaviour.CirculationBehaviour.Source.transform);
                indexInSource = limbBehaviour.CirculationBehaviour.Source.PushesTo.ToList().IndexOf(limbBehaviour.CirculationBehaviour);
            }
            else
            {
                sourcePath = "";
                indexInSource = 0;
            }
            localScale = limbBehaviour.transform.localScale;
            nodeInformation = new NodeInformation(limbBehaviour);
            if (limbBehaviour.NearestLimbToBrain != null)
            {
                nearestLimbToBrainPath = Utility.GetHierarchyPath(limbBehaviour.NearestLimbToBrain.transform.root, limbBehaviour.NearestLimbToBrain.transform);
            }
            else
            {
                nearestLimbToBrainPath = "";
            }
            try
            {
                indexInSerialiseInstructions = limbBehaviour.Person.gameObject.GetComponent<SerialiseInstructions>().RelevantTransforms.ToList().IndexOf(limbBehaviour.transform);
            }
            catch
            {
                indexInSerialiseInstructions = -1;
            }
            posesInformation = new PoseInformation[limbBehaviour.Person.Poses.Count];
            for (int i = 0; i < limbBehaviour.Person.Poses.Count; i++)
            {
                posesInformation[i] = new PoseInformation(limbBehaviour.Person.Poses[i], limbBehaviour);
            }
            originalJointLimits = limbBehaviour.OriginalJointLimits;
            adjacentLimbs = new string[limbBehaviour.SkinMaterialHandler.adjacentLimbs.Length];
            for (int i = 0; i < adjacentLimbs.Length; i++)
            {
                adjacentLimbs[i] = Utility.GetHierarchyPath(limbBehaviour.SkinMaterialHandler.adjacentLimbs[i].transform.root, limbBehaviour.SkinMaterialHandler.adjacentLimbs[i].transform);
            }
            distanceBrain = limbBehaviour.DistanceToBrain;
            if (limbBehaviour.TryGetComponent<ShatteredObjectGenerator>(out ShatteredObjectGenerator shatteredObjectGenerator) && shatteredObjectGenerator.ConnectTo != null)
            {
                shatteredConnectedTo = Utility.GetHierarchyPath(shatteredObjectGenerator.ConnectTo.transform.root, shatteredObjectGenerator.ConnectTo.transform);
            }
            else
            {
                shatteredConnectedTo = "";
            }
            try
            {
                var shatteredParentLimb = limbBehaviour.Person.Limbs.Where(limb => limb.GetComponent<ShatteredObjectGenerator>().ConnectTo == limbBehaviour.PhysicalBehaviour.rigidbody).FirstOrDefault();
                shatteredParent = Utility.GetHierarchyPath(shatteredParentLimb.transform.root, shatteredParentLimb.transform);
            }
            catch
            {
                shatteredParent = "";
            }
            var allGoreStrings = limbBehaviour.Person.Limbs.Where(limb => limb.GetComponent<GoreStringBehaviour>() != null);
            var limbGoreStrings = allGoreStrings.Where(goreString => goreString.GetComponent<GoreStringBehaviour>().Other == limbBehaviour.PhysicalBehaviour.rigidbody).ToArray();
            goreStringsPaths = new string[limbGoreStrings.Count()];
            for (int i = 0; i < limbGoreStrings.Count(); i++)
            {
                goreStringsPaths[i] = Utility.GetHierarchyPath(limbGoreStrings[i].transform.root, limbGoreStrings[i].transform);
            }
            var limbSpriteRenderer = limbBehaviour.PhysicalBehaviour.spriteRenderer;
            var limbMaterial = limbSpriteRenderer.material;
            sortingLayerName = limbSpriteRenderer.sortingLayerName;
            sortingOrder = limbSpriteRenderer.sortingOrder;
            isZombie = limbBehaviour.IsZombie;
            breakingThresold = limbBehaviour.BreakingThreshold;
            MotorStrength = limbBehaviour.MotorStrength;
        }
        public string name;
        public string transformPath;
        public string parentTransformPath;
        //circulation
        public string[] connectedLimbsPath;
        public string[] pushToLimbsPath;
        public string sourcePath;
        public int indexInSource;
        //
        public string nearestLimbToBrainPath;
        public int orderInPerson;
        public Vector3 localScale;
        public Vector2 originalJointLimits;
        public HingeJointInformation hingeJointInformation;
        public NodeInformation nodeInformation;
        public PoseInformation[] posesInformation;
        public int indexInSerialiseInstructions;
        public string[] adjacentLimbs;
        public int distanceBrain;
        public float breakingThresold;
        //
        public string shatteredConnectedTo;
        public string shatteredParent;
        //
        public string[] goreStringsPaths;
        public string sortingLayerName;
        public int sortingOrder;
        public bool isZombie;

        public float MotorStrength;
    }
    public struct PoseInformation
    {
        public PoseInformation(RagdollPose ragdollPose, LimbBehaviour limbBehaviour)
        {
            poseIndexInPerson = limbBehaviour.Person.Poses.IndexOf(ragdollPose);
            if (ragdollPose.Angles.Select(angle => angle.Limb).Contains(limbBehaviour))
            {
                var limbPose = ragdollPose.Angles.Where(angle => angle.Limb == limbBehaviour).FirstOrDefault();
                limbIndexInPose = ragdollPose.Angles.IndexOf(limbPose);
            }
            else
            {
                limbIndexInPose = -1;
            }
        }
        public int poseIndexInPerson;
        public int limbIndexInPose;
    }
    public struct NodeInformation
    {
        public NodeInformation(LimbBehaviour limbBehaviour)
        {
            connectionsTransformPaths = new string[limbBehaviour.NodeBehaviour.Connections.Length];
            indexInConnectetions = new Dictionary<string, int>();
            for (int i = 0; i < limbBehaviour.NodeBehaviour.Connections.Length; i++)
            {
                connectionsTransformPaths[i] = Utility.GetHierarchyPath(limbBehaviour.NodeBehaviour.Connections[i].transform.root, limbBehaviour.NodeBehaviour.Connections[i].transform);
            }
            foreach (var connectedNode in limbBehaviour.NodeBehaviour.Connections)
            {
                indexInConnectetions.Add(Utility.GetHierarchyPath(connectedNode.transform.root, connectedNode.transform), connectedNode.Connections.ToList().IndexOf(limbBehaviour.NodeBehaviour));
            }
            isRoot = limbBehaviour.NodeBehaviour.IsRoot;
            value = limbBehaviour.NodeBehaviour.Value;
        }
        public string[] connectionsTransformPaths;
        public bool isRoot;
        public int value;
        public Dictionary<string, int> indexInConnectetions;
    }
    public struct HingeJointInformation
    {
        public HingeJointInformation(LimbBehaviour limbBehaviour)
        {
            if (limbBehaviour.Joint != null)
            {
                hasInformation = true;
                jointAngleLimits = limbBehaviour.Joint.limits;
                connectedAnchor = limbBehaviour.Joint.connectedAnchor;
                anchor = limbBehaviour.Joint.anchor;
                connectedBodyPath = Utility.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.Joint.connectedBody.transform);
                connectedBodyRotation = limbBehaviour.Joint.connectedBody.transform.rotation;
                attachedBodyPath = Utility.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.Joint.attachedRigidbody.transform);
                useLimits = limbBehaviour.Joint.useLimits;
                breakForce = limbBehaviour.Joint.breakForce;
                breakTorque = limbBehaviour.Joint.breakTorque;
            }
            else
            {
                hasInformation = false;
                jointAngleLimits = new JointAngleLimits2D();
                connectedAnchor = new Vector2();
                anchor = new Vector2();
                connectedBodyPath = "";
                attachedBodyPath = "";
                connectedBodyRotation = new Quaternion();
                useLimits = false;
                breakForce = 0;
                breakTorque = 0;
            }
            var findedAttachedHingePath = limbBehaviour.ConnectedLimbs.Where(connectedLimb =>
            {
                if (connectedLimb.Joint != null)
                {
                    if (connectedLimb.Joint.connectedBody.transform == limbBehaviour.transform)
                    {
                        return true;
                    }
                }
                return false;
            }).ToArray();
            attachedHingePaths = new string[findedAttachedHingePath.Length];
            for (int i = 0; i < findedAttachedHingePath.Length; i++)
            {
                attachedHingePaths[i] = Utility.GetHierarchyPath(findedAttachedHingePath[i].transform.root, findedAttachedHingePath[i].transform);
            }
        }
        public bool hasInformation;
        public JointAngleLimits2D jointAngleLimits;
        public Vector2 connectedAnchor;
        public Vector2 anchor;
        public string connectedBodyPath;
        public string attachedBodyPath;
        public Quaternion connectedBodyRotation;
        public bool useLimits;
        public float breakForce;
        public float breakTorque;

        public string[] attachedHingePaths;
    }
    #endregion
    #region DebugClasses
    public class PointDebuggerCircle : MonoBehaviour
    {
        public float Radius = 0.3f;
        private void Update()
        {
            ModAPI.Draw.Circle(gameObject.transform.position, Radius);
        }
    }
    public class PointDebuggerVector : MonoBehaviour
    {
        public float Radius = 0.3f;
        public Vector2 Vector;
        private void Update()
        {
            ModAPI.Draw.Circle(Vector, Radius);
        }
    }
    public class ActOnBeforeSerialize : MonoBehaviour
    {
        public EventHandler EventHandler;
        public void OnBeforeSerialise()
        {
            EventHandler(this, null);
        }
    }
    public class PointDebuggerCollider : MonoBehaviour
    {
        public Collider2D Collider;
        private void Update()
        {
            ModAPI.Draw.Collider(Collider);
        }
    }
    public class HingeJointDebugger : MonoBehaviour
    {
        public HingeJoint2D Joint;
        public float Radius = 0.01f;
        public void Update()
        {
            if (Joint)
            {
                ModAPI.Draw.Circle(Joint.connectedBody.transform.TransformPoint(Joint.connectedAnchor), Radius);
                ModAPI.Draw.Circle(Joint.gameObject.transform.TransformPoint(Joint.anchor), Radius);
            }
        }
    }
    #endregion
    #region OtherClasses
    public struct SerializedObjects
    {
        public List<ObjectState> Objects;
    }
    public class FreezerDrag : MonoBehaviour
    {
        public FreezeBehaviour freezeBehaviour;
        private void Start()
        {
            OnMouseUp();
        }
        private void OnMouseUp()
        {
            freezeBehaviour = gameObject.GetOrAddComponent<FreezeBehaviour>();
        }
        private void OnMouseDown()
        {
            UnityEngine.Object.Destroy(freezeBehaviour);
        }
    }
    public class PID
    {
        public float Kp, Ki, Kd;

        private float lastError;
        private float P, I, D;

        public PID()
        {
            Kp = 1f;
            Ki = 0;
            Kd = 0.2f;
        }

        public PID(float pFactor, float iFactor, float dFactor)
        {
            this.Kp = pFactor;
            this.Ki = iFactor;
            this.Kd = dFactor;
        }

        public float Update(float error, float dt)
        {
            P = error;
            I += error * dt;
            D = (error - lastError) / dt;
            lastError = error;

            float CO = P * Kp + I * Ki + D * Kd;

            return CO;
        }
    }
    public class ActOnCollideOwn : MonoBehaviour
    {
        public float ImpactForceThreshold;
        public float DispatchChance = 0.5f;
        public Action<Collision2D> Actions;
        public bool Debug = false;
        private void OnCollisionEnter2D(Collision2D coll)
        {
            ContactPoint2D[] contacts = coll.contacts;
            var averageImpact = Utils.GetAverageImpulse(contacts, contacts.Length);
            if (Debug)
            {
                UnityEngine.Debug.Log(averageImpact);
            }
            if (UnityEngine.Random.Range(0f, 1f) > DispatchChance)
            {
                return;
            }
            if (averageImpact > ImpactForceThreshold)
            {
                this.Actions.Invoke(coll);
            }
        }
    }
    internal class InvokerOnStart : MonoBehaviour
    {
        public Action ActionForInvoke;

        public void Start()
        {
            ActionForInvoke.Invoke();
            Destroy(this);
        }
    }
    public class ForceAngle : MonoBehaviour
    {
        private float TargetAngle;
        private LimbBehaviour LimbToCopy;
        private LimbBehaviour limb;
        private int lastDirection;
        private float startRotation = 0f;
        private void Start()
        {
            limb = GetComponent<LimbBehaviour>();
            startRotation = LimbToCopy.PhysicalBehaviour.rigidbody.rotation;
            lastDirection = transform.lossyScale.x > 0 ? 1 : -1;
            limb.PhysicalBehaviour.rigidbody.SetRotation(startRotation + TargetAngle * lastDirection);
            limb.Joint.connectedBody = limb.Joint.connectedBody;
        }
        private void Update()
        {
            int direction = transform.lossyScale.x > 0 ? 1 : -1;
            if(lastDirection != direction)
            {
                Setup(TargetAngle * direction, limb);
                limb.PhysicalBehaviour.rigidbody.SetRotation(startRotation + TargetAngle * direction);
            }
            lastDirection = direction;
        }
        public void Setup(float TargetAngle, LimbBehaviour LimbToCopy)
        {
            this.TargetAngle = TargetAngle;
            this.LimbToCopy = LimbToCopy;
        }
    }
    internal class InvokerAfterDelay : MonoBehaviour
    {
        public Action ActionForInvoke;
        public float Delay;
        private void Start()
        {
            StartCoroutine(Delayer());
        }
        private IEnumerator Delayer()
        {
            yield return new WaitForSeconds(Delay);
            ActionForInvoke.Invoke();
            Destroy(gameObject);
        }
    }
    internal class Dont : MonoBehaviour
    {
        private void OnDisable()
        {
            gameObject.BetterDestroy<Dont>();
            gameObject.GetOrAddComponent<Dont>();
        }
    }
    [SkipSerialisation]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    internal class AnimatedImage : MonoBehaviour
    {
        internal Sprite[] Sprites;
        internal UnityEngine.UI.Image Image;
        private int Local = 0;
        private float T = 0;
        public float TimeFrame = 0.125f;
        private void Start() { Image = gameObject.GetComponent<UnityEngine.UI.Image>(); }
        private void FixedUpdate()
        {
            T += Time.fixedDeltaTime;
            if (T > TimeFrame)
            {
                Local += Local >= Sprites.Count() - 1 ? -Local : 1;
                Image.sprite = Sprites[Local];
                T = 0;
            }
        }
    }
    [SkipSerialisation]
    [RequireComponent(typeof(SpriteRenderer))]
    internal class AnimatedSprite : MonoBehaviour
    {
        internal Sprite[] Sprites;
        private int local = 0;
        private float T = 0;
        public float Speed;
        private void FixedUpdate()
        {
            T += Time.fixedDeltaTime;
            if (T > Speed)
            {
                local += local >= Sprites.Count() - 1 ? -local : 1;
                gameObject.AdvancedSpriteChange(Sprites[local], true, false);
                T = 0;
            }
        }
    }
    public abstract class IDWithLiqud : Liquid
    {
        public static string ID;
    }
    public class CustomButtonBehaviour : MonoBehaviour
    {
        public string TargetCategory;
        public GameObject TargetButton;
        private void Update()
        {
            if (CatalogBehaviour.Main.SelectedCategory.name == TargetCategory && !TargetButton.activeInHierarchy)
            {
                TargetButton.SetActive(true);
            }
            else if (CatalogBehaviour.Main.SelectedCategory.name != TargetCategory && TargetButton.activeInHierarchy)
            {
                TargetButton.SetActive(false);
            }
        }
    }
    public class StabberFixer : MonoBehaviour
    {
        public SharpAxis SharpAxis
        {
            set
            {
                needChangeSharpAxis = true;
                m_SharpAxis = value;
            }
        }
        private bool needChangeSharpAxis = false;
        private SharpAxis m_SharpAxis;
        private PhysicalBehaviour physicalBehaviour;
        public Action<BoxCollider2D> OnFixed;
        private void Start()
        {
            physicalBehaviour = gameObject.GetComponent<PhysicalBehaviour>();
            StartCoroutine(FixCor());
        }
        private IEnumerator FixCor()
        {
            yield return new WaitForEndOfFrame();
            if (needChangeSharpAxis)
            {
                var newProperties = Instantiate(physicalBehaviour.Properties);
                newProperties.name += "Changed";
                newProperties.Sharp = true;
                newProperties.SharpAxes = new SharpAxis[] { m_SharpAxis };
                physicalBehaviour.Properties = newProperties;
            }
            foreach (var collider in gameObject.GetComponents<Collider2D>())
            {
                Destroy(collider);
            }
            var boxCollider = gameObject.AddComponent<BoxCollider2D>();
            yield return new WaitForEndOfFrame();
            physicalBehaviour.ResetColliderArray();
            yield return new WaitForEndOfFrame();
            physicalBehaviour.BakeColliderGridPoints();
            if (OnFixed != null)
            {
                OnFixed.Invoke(boxCollider);
            }
        }
    }
    public class ParticleSystemShaderFixer : MonoBehaviour
    {
        private void Start()
        {
            var particleSystemRenderers = gameObject.GetComponents<ParticleSystemRenderer>();
            foreach (var psr in particleSystemRenderers)
            {
                foreach (var mat in psr.sharedMaterials)
                {
                    mat.shader = Shader.Find(mat.shader.name);
                    mat.name = mat.name + "Fixed";
                }
            }
        }
    }
    public class PseudoChild : MonoBehaviour
    {
        [SkipSerialisation]
        public Transform Parent;
        public bool RotationSync = true;
        public bool ScaleSync = true;
        public Vector2 positionOffset;
        public float rotationOffset;
        public float DeleteAfterParentDestroy
        {
            set
            {
                needDestroy = true;
                m_deleteAfterParentDestroy = value;
            }
        }
        private float m_deleteAfterParentDestroy;
        private bool destroyed = false;
        private bool needDestroy = false;
        private void Update()
        {
            if (Parent == null)
            {
                if (!destroyed)
                {
                    StartCoroutine(DestroyAction());
                }
            }
            else
            {
                transform.position = Parent.position + transform.TransformVector(positionOffset);
                if (ScaleSync)
                    transform.localScale = Parent.localScale;
                if (RotationSync)
                {
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + rotationOffset);
                }
            }
        }
        private IEnumerator DestroyAction()
        {
            destroyed = true;
            if (needDestroy)
            {
                yield return new WaitForSeconds(m_deleteAfterParentDestroy);
                gameObject.Destroy();
            }
        }
    }
    #endregion
}