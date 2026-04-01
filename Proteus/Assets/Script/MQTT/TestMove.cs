using UnityEngine;

public class TestMove : MonoBehaviour
{
    void Update()
    {
        // Force rotate + move
        transform.Rotate(0, 20 * Time.deltaTime, 0);
        transform.Translate(0, 0, Time.deltaTime * 2);
    }
}