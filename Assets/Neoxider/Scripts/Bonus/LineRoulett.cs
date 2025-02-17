using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo
{
    namespace Bonus
    {
        [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(LineRoulett))]
        public class LineRoulett : MonoBehaviour
        {
            [SerializeField] private Transform arrow;
            [SerializeField] private Image[] images;
            [SerializeField] public Sprite[] sprites;

            [SerializeField] private float speed = 15;
            [SerializeField] private float timeRoll = 3;
            [SerializeField] private float slowDownTime = 0.5f;

            private Coroutine rollCoroutine;

            private int idWin;
            private Image winningImage;

            [SerializeField] private float space;
            [SerializeField] private float resetX;

            [Space]
            public UnityEvent<int> OnWin;

            [Space, Header("Update Visual for all images")]

            public bool updateSetting;

            void Start()
            {
                UpdateVisual();

                for (int i = 0; i < images.Length; i++)
                {
                    images[i].sprite = GetRandomSprite();
                }
            }

            public void StartRolling()
            {
                idWin = -1;

                if (rollCoroutine != null)
                {
                    StopCoroutine(rollCoroutine);
                }

                rollCoroutine = StartCoroutine(Roll());
            }

            private Sprite GetRandomSprite()
            {
                return sprites[Random.Range(0, sprites.Length)];
            }

            private IEnumerator Roll()
            {
                float timer = 0;

                while (timer < timeRoll)
                {
                    Move(speed);

                    timer += Time.deltaTime;
                    yield return null;
                }

                yield return StartCoroutine(SlowDown());
            }

            private void Move(float speed)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    images[i].transform.position += speed * Time.deltaTime * Vector3.left;

                    if (images[i].transform.position.x < resetX)
                    {
                        Vector3 pos = images[i].transform.position;
                        pos.x = GetLastPos().x + space;
                        images[i].transform.position = pos;
                        images[i].sprite = GetRandomSprite();
                    }
                }
            }

            private IEnumerator SlowDown()
            {
                slowDownTime = 2f;
                float elapsedTime = 0;
                float speed = this.speed;

                while (elapsedTime < slowDownTime)
                {
                    float t = elapsedTime / slowDownTime;
                    speed = Mathf.Lerp(speed, 0, t);

                    Move(speed);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                DetermineWinningImage();

                rollCoroutine = null;
            }

            private Vector3 GetLastPos()
            {
                Vector3 last = images[0].transform.position;

                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].transform.position.x > last.x)
                        last = images[i].transform.position;
                }

                return last;
            }

            private void DetermineWinningImage()
            {
                winningImage = null;
                float minDistance = float.MaxValue;

                foreach (var image in images)
                {
                    float distance = Mathf.Abs(image.transform.position.x - arrow.position.x);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        winningImage = image;
                    }
                }

                if (winningImage != null)
                {
                    idWin = CheckSpriteId(winningImage.sprite);
                    OnWin?.Invoke(idWin);
                }

                print("Win = " + idWin);
            }

            private int CheckSpriteId(Sprite sprite)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == sprite)
                        return i;
                }

                return -1;
            }

            private void OnValidate()
            {
                if (updateSetting)
                {
                    UpdateVisual();
                }
            }

            private void UpdateVisual()
            {
                updateSetting = false;

                space = images[1].transform.position.x - images[0].transform.position.x;
                resetX = images[1].transform.position.x;

                for (int i = 1; i < images.Length; i++)
                {
                    Vector3 pos = images[i - 1].transform.position;
                    pos.x += space;
                    images[i].transform.position = pos;
                    images[i].sprite = GetRandomSprite();
                }
            }
        }
    }
}