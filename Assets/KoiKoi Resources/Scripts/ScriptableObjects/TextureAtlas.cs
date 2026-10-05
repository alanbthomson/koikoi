using UnityEngine;

[CreateAssetMenu(fileName = "KoiKoiCardImages", menuName = "Scriptable Objects/KoiKoiCardImages")]
public class TextureAtlas : ScriptableObject {
    [SerializeField] private Texture2D[] orderedCardImages;

    public Texture2D GetTexture(int id) {
        return (0 <= id && id < orderedCardImages.Length) ? orderedCardImages[id] : null;
    }
}
