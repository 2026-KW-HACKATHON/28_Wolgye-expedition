// CharacterData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Shop/Character Data")]
public class CharacterData : ShopItemBase
{
    [SerializeField] private GameObject characterPrefab;

    public GameObject CharacterPrefab => characterPrefab;
    public override ShopCategory Category => ShopCategory.Character;
}