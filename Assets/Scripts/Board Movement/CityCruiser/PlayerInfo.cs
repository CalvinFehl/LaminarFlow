using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public int playerID;

    // Überschreibe die Equals Methode, um basierend auf der playerID zu vergleichen
    public override bool Equals(object obj)
    {
        if (obj is PlayerInfo otherPlayer)
        {
            return playerID == otherPlayer.playerID;
        }
        return false;
    }

    // Der Hashcode wird auf der playerID basieren
    public override int GetHashCode()
    {
        return playerID.GetHashCode();
    }
}
