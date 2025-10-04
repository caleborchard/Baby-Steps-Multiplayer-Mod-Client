using Il2CppTMPro;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Player
{
    public class NameTagUI
    {
        public GameObject baseObject;
        public TextMeshPro textMeshPro;

        public Vector3 positionOffset = new Vector3(0, 0.5f, 0);

        public NameTagUI(Transform parent)
        {
            baseObject = new GameObject("Nametag");
            SetParent(parent);

            textMeshPro = baseObject.AddComponent<TextMeshPro>();
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.fontSize = 1.5f;

            Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (overlayShader != null)
            {
                // Create a new material with the overlay shader
                Material overlayMat = new Material(overlayShader);
                overlayMat.mainTexture = textMeshPro.font.material.mainTexture; // Keep glyph atlas
                textMeshPro.fontMaterial = overlayMat;
            }
            else
            {
                Core.logger.Warning("Could not find TMP Distance Field Overlay shader!");
            }

            SetColor(Color.white);
            SetText("Nate");
        }

        public void Destroy()
        {
            if (baseObject != null)
                GameObject.Destroy(baseObject);
        }

        public void LateUpdate()
        {
            if (baseObject == null)
                return;

            if ((LocalPlayer.Instance != null)
                && (LocalPlayer.Instance.camera != null))
            {

                float distance = Vector3.Distance(LocalPlayer.Instance.camera.position, baseObject.transform.position);
                SetActive(distance <= 100f);
                RotateTowardsCamera(LocalPlayer.Instance.camera);
            }
        }

        public void SetActive(bool active)
        {
            if (baseObject == null)
                return;
            if (baseObject.active == active)
                return;
            baseObject.active = active;
        }

        public void SetParent(Transform parent)
        {
            if (baseObject == null)
                return;

            baseObject.transform.SetParent(parent, false);
            baseObject.transform.localPosition = positionOffset;

            if ((LocalPlayer.Instance != null)
                && (LocalPlayer.Instance.camera != null)
                && baseObject.activeSelf)
                RotateTowardsCamera(LocalPlayer.Instance.camera);
        }

        public void SetText(string text)
        {
            if (textMeshPro == null)
                return;
            if (textMeshPro.text == text)
                return;
            textMeshPro.text = text;
            textMeshPro.SetAllDirty();
        }

        public void SetColor(Color color)
        {
            if (textMeshPro == null)
                return;
            if (textMeshPro.color == color)
                return;
            textMeshPro.color = color;
            textMeshPro.SetAllDirty();
        }

        public Color GetColor()
        {
            if (textMeshPro == null)
                return new();
            return textMeshPro.color;
        }

        private void RotateTowardsCamera(Transform camera)
        {
            if (camera == null)
                return;
            if (baseObject == null)
                return;
            if (!baseObject.activeSelf)
                return;
            baseObject.transform.rotation = 
                Quaternion.LookRotation(baseObject.transform.position - camera.transform.position);
        }
    }
}
