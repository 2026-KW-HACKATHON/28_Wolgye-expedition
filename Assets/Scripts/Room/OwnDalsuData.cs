using UnityEngine;

public class OwnDalsuData : MonoBehaviour
{
    public DalsuData dalsuData;
    public int listIndex;

    // data.json에 저장된 원본 레코드. 돌려보내기 등에서 이 레코드를 식별/삭제할 때 사용합니다.
    public OwnDalsu ownData;
}
