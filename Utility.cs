using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Reflection;

namespace Utility
{
    public static class Utility
    {
        public static Vector3 GetModuleVector(this Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
        }
        public static void SetPositionWithOffSet(Transform child, Transform parent, float offSetX = 0, float offSetY = 0)
        {
            child.position = parent.position;
            child.localPosition = new Vector3(offSetX, offSetY);
            child.localRotation = Quaternion.identity;
        }
        public static JointMotor2D SetMotorForce(JointMotor2D jointMotor2D, float force, float torque = 10000f)
        {
            jointMotor2D.motorSpeed = force;
            jointMotor2D.maxMotorTorque = torque;
            return jointMotor2D;
        }
        public static void DrawCircle(this GameObject gameObject, float radius = 0.3f)
        {
            var pointDebugger = gameObject.AddComponent<PointDebuggerCircle>();
            pointDebugger.radius = radius;
        }
        public static void DrawVector(this Vector2 vector, float radius = 0.3f)
        {
            var circleHandler = new GameObject("colliderHandler");
            var pointBehaviour = circleHandler.AddComponent<PointDebuggerVector>();
            pointBehaviour.radius = radius;
            pointBehaviour.vector = vector;
        }
        public static bool IsColissionDisabled(this GameObject gameObject)
        {
            return gameObject.layer == 10 ? true : false;
        }
        public static void DrawCollider(this GameObject gameObject)
        {
            gameObject.AddComponent<PointDebuggerCollider>().collider = gameObject.GetComponent<Collider2D>();
        }
        public static void DrawCollider(this Collider2D collider)
        {
            var colliderHandler = new GameObject("colliderHandler");
            colliderHandler.AddComponent<PointDebuggerCollider>().collider = collider;
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
        public static
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
        public static void InitializePhysicalComponent(this GameObject gameObject)
        {
            gameObject.layer = LayerMask.NameToLayer("Objects");
            gameObject.AddComponent<Rigidbody2D>();
            PhysicalBehaviour physicalBehaviour = gameObject.AddComponent<PhysicalBehaviour>();
            physicalBehaviour.Properties = ModAPI.FindPhysicalProperties("Metal");
            physicalBehaviour.SpawnSpawnParticles = false;
            physicalBehaviour.OverrideShotSounds = Array.Empty<AudioClip>();
            physicalBehaviour.OverrideImpactSounds = Array.Empty<AudioClip>();
        }
        public static void OpenLink(string url)
        {
            Type type = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule");
            type.GetMethod("OpenURL", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Invoke(null, new object[] { url });
        }
        public static void ChangeSpecificLimbSprite(this LimbBehaviour limbBehaviour, Sprite skin, Sprite flash, Sprite bone, Sprite damage)
        {
            var limbSpriteRenderer = limbBehaviour.GetComponent<SpriteRenderer>();
            limbSpriteRenderer.sprite = skin;
            limbSpriteRenderer.material.SetTexture("_FleshTex", flash.texture);
            limbSpriteRenderer.material.SetTexture("_BoneTex", bone.texture);
            limbSpriteRenderer.material.SetTexture("_DamageTex", damage.texture);
        }
        public static PhysicalProperties GetBoundsPhysicalProperties()
        {
            var map = MapRegistry.GetMap("fb813068-e717-45de-a97f-4677a41758e6");
            return map.Prefab.transform.Find("Root/Left wall").GetComponent<PhysicalBehaviour>().Properties;
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
    }
    public class PointDebuggerCircle : MonoBehaviour
    {
        public float radius = 0.3f;
        private void Update()
        {
            ModAPI.Draw.Circle(gameObject.transform.position, radius);
        }
    }
    public class PointDebuggerVector : MonoBehaviour
    {
        public float radius = 0.3f;
        public Vector2 vector;
        private void Update()
        {
            ModAPI.Draw.Circle(vector, radius);
        }
    }
    public class PointDebuggerCollider : MonoBehaviour
    {
        public Collider2D collider;
        private void Update()
        {
            ModAPI.Draw.Collider(collider);
        }
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
    public class ActOnCollideOwn : MonoBehaviour
    {
        public float ImpactForceThreshold;
        public float DispatchChance = 0.5f;
        public Action<Collision2D> Actions;
        public bool debug = false;
        private void OnCollisionEnter2D(Collision2D coll)
        {
            ContactPoint2D[] contacts = coll.contacts;
            var averageImpact = Utils.GetAverageImpulse(contacts, contacts.Length);
            if (debug)
            {
                Debug.Log(averageImpact);
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

}