using UnityEngine;

namespace Backrooms
{
    [RequireComponent(typeof(Renderer))]
    public class WaterSurface : MonoBehaviour
    {
        public Vector2 flow = new Vector2(0.012f, 0.02f);
        public float tiling = 0.4f;

        Renderer target;
        MaterialPropertyBlock block;
        static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            var offset = flow * Time.time;
            target.GetPropertyBlock(block);
            block.SetVector(BaseMapSt, new Vector4(tiling, tiling, offset.x, offset.y));
            target.SetPropertyBlock(block);
        }
    }
}
