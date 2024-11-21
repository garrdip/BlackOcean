using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gpm.Ui;

[System.Serializable]
public class CardQueue : InfiniteScrollData
{
    public uint cardOwnerNetId;
    public Card card;
    public bool isCurrent = false;
}
