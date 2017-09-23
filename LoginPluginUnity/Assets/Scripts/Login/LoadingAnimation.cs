using UnityEngine;

namespace Login
{
    public class LoadingAnimation : MonoBehaviour
    {
        public GameObject Particles;

        private const float Speed = 360f;
        private const float Radius = 0.5f;
        private Vector3 _offset;
        private Transform _transform;
        private Transform _particleTransform;
        private bool _isAnimating;
        private Vector3 _particleStartPosition;


        private void Awake()
        {
            _particleTransform = Particles.GetComponent<Transform>();
            _particleStartPosition = _particleTransform.position;
            _transform = GetComponent<Transform>();
        }

        private void Update()
        {
            if (_isAnimating)
            {
                _transform.Rotate(0f, 0f, Speed * Time.deltaTime);
                _particleTransform.localPosition = Vector3.MoveTowards(_particleTransform.localPosition, _offset, 0.5f * Time.deltaTime);
            }
        }

        public void StartLoaderAnimation()
        {
            _isAnimating = true;
            _offset = new Vector3(Radius, 0f, 0f);
            Particles.SetActive(true);
        }

        public void StopLoaderAnimation()
        {
            _isAnimating = false;
            Particles.SetActive(false);
            Particles.transform.position = _particleStartPosition;
        }
    }
}