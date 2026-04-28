using System.Diagnostics;
using UnityEngine;

public class Player : MonoBehaviour
{
    
   public int hp;
   public int power;
   void Damage(int hp)
    {
        this.hp -= hp;
        print("Player damaged! Current HP: " + this.hp);
        print("Player's hp: " + hp);
    }
 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Damage(10);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
