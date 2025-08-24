using UnityEngine;

namespace Rubik
{
    public enum Face { Up, Down, Left, Right, Front, Back }

    // each sticker on the cube
    public class StickerTag : MonoBehaviour
    {
        public Face face;
        public Cubelet cubelet;   // parent cubelet
        public Vector3 normalWS;  // world space normal (for rotation detection)
    }
}