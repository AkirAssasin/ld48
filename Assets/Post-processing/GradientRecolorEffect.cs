using UnityEngine;
using System.Collections;
using System.Linq;

public class GradientRecolorEffect : MonoBehaviour {	

    public class Blob {

        public Vector2 position;
        public Vector2 velocity;

        public Blob (Vector2 _position, Vector2 _velocity) {

            position = _position;
            velocity = _velocity;

        }

    }

    [Header("Shader")]

    public Shader shader;
    public Texture rampTexture;
    public float blobMaxSpeed;
    public float blobAcceleration;
    public float blobRange;

    Material material;

    Blob[] blobs;

    new Camera camera;
    float effectWidth;


    void Awake () {

        camera = GetComponent<Camera>();

        material = new Material(shader);
        material.SetTexture("_RampTex",rampTexture);

        material.SetFloat("_BlobMin", Random.value);
        material.SetFloat("_BlobRange", blobRange);

        blobParamCache = new Vector4[3];
        blobs = new Blob[blobParamCache.Length];

        for (int i = 0; i < blobs.Length; ++i) {

            Vector2 position = new Vector2(Random.value,Random.value);
            blobs[i] = new Blob(position,Random.insideUnitCircle * blobMaxSpeed);

        }

        UpdateParams();

    }

    public Vector4[] blobParamCache;

    void Update () {


        float dt = Time.deltaTime;

        effectWidth = camera.aspect;

        for (int i = 0; i < blobs.Length; ++i) {

            Blob blob = blobs[i];

            blob.position += blob.velocity * dt;
            blob.velocity += Random.insideUnitCircle * (blobAcceleration * dt);
            Vector2.ClampMagnitude(blob.velocity,blobMaxSpeed);

            if (blob.position.x < 0) {
                blob.position.x = 0;
                blob.velocity.x *= -1;
            }

            if (blob.position.x > effectWidth) {
                blob.position.x = effectWidth;
                blob.velocity.x *= -1;
            }

            if (blob.position.y < 0) {
                blob.position.y = 0;
                blob.velocity.y *= -1;
            }

            if (blob.position.y > 1) {
                blob.position.y = 1;
                blob.velocity.y *= -1;
            }

        }

        UpdateParams();

    }

    void UpdateParams () {

        for (int i = 0; i < blobs.Length; ++i) {

            Blob blob = blobs[i];
            blobParamCache[i] = new Vector4(blob.position.x,blob.position.y,1,1);

        }

        material.SetVectorArray("_Blobs",blobParamCache);
        material.SetFloat("_WidthScale",effectWidth);

    }

    void OnRenderImage (RenderTexture source, RenderTexture destination) {
	
        Graphics.Blit(source,destination,material);

	}
}