#if TEXT_MESH_PRO
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DummyScript : MonoBehaviour
{
    private TextMeshProUGUI dummyText;

    private void OnValidate()
    {
        if (!dummyText)
        {
            dummyText = GetComponent<TextMeshProUGUI>();
        }

        if (!dummyText)
        {
            throw new MissingReferenceException(nameof(dummyText));
        }
    }

    private void Awake() => OnValidate();
}
#endif
