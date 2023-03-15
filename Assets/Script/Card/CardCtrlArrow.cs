using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

public class CardCtrlArrow : NetworkBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public GameObject arrowHeadPrefab;
    public GameObject arrowNodePrefab;

    public int arrowNodeNum;
    public float scaleFactor = 1f;

    private RectTransform origin;
    private List<RectTransform> arrowNodes = new List<RectTransform>();
    private List<Vector2> controlPoints = new List<Vector2>();
    private readonly List<Vector2> controlPointFactors = new List<Vector2>{ new Vector2(-0.3f, 0.8f), new Vector2(0.1f, 1.4f) };

    void Awake()
    {
        this.origin = this.GetComponent<RectTransform>();
        for(int i=0; i<this.arrowNodeNum; i++){
            this.arrowNodes.Add(Instantiate(this.arrowNodePrefab, this.transform).GetComponent<RectTransform>());
        }
        this.arrowNodes.Add(Instantiate(this.arrowHeadPrefab, this.transform).GetComponent<RectTransform>());

        this.arrowNodes.ForEach(a => a.GetComponent<RectTransform>().position = new Vector2(-1000, -1000));

        for(int i=0; i<4; i++){
            this.controlPoints.Add(Vector2.zero);
        }
    }

    void Update()
    {
        // P0 is at the arrow emitter point
        this.controlPoints[0] = new Vector2(this.origin.position.x, this.origin.position.y);
        
        // P3 is at the mouse position
        this.controlPoints[3] = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        // P1, P2 determines by P0 and P3
        // P1 = P0 + (P3 - P0) * Vector2(-0.3f, 0.8f)
        // P2 = P0 + (P3 - P0) * Vector2(0.1f, 1.4f)
        this.controlPoints[1] = this.controlPoints[0] + (this.controlPoints[3] - this.controlPoints[0]) * this.controlPointFactors[0];
        this.controlPoints[2] = this.controlPoints[0] + (this.controlPoints[3] - this.controlPoints[0]) * this.controlPointFactors[1];

        for(int i=0; i<this.arrowNodes.Count; ++i){
            // Caculates t.
            var t = Mathf.Log(1f * i / (this.arrowNodes.Count -1) + 1f, 2f);
            
            // Cubic Bezier Curve
            // B(t) = (1-t)^3 * P0 + 3 * (1-t)^2 * t * P1 + 3 * (1-t) * t^2 * P2 + t^3 * P3
            this.arrowNodes[i].position =
                Mathf.Pow(1 - t, 3) * this.controlPoints[0] +
                3 * Mathf.Pow(1 - t, 2) * t * this.controlPoints[1] +
                3 * (1 - t) * Mathf.Pow(t, 2) * this.controlPoints[2] + 
                Mathf.Pow(t, 3) * this.controlPoints[3];

            // Caculates rotations for each arrow node.
            if(i > 0){
                var euler = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, this.arrowNodes[i].position - this.arrowNodes[i - 1].position));
                this.arrowNodes[i].rotation = Quaternion.Euler(euler);
            }
            // Calculate scales for each arrow node
            var scale = this.scaleFactor * (1f - 0.03f * (this.arrowNodes.Count -1 -i));
            this.arrowNodes[i].localScale = new Vector3(scale, scale, 1f);
        }

        // The first arrow node's rotation
        this.arrowNodes[0].transform.rotation = this.arrowNodes[1].transform.rotation;
    } 

    public void OnPointerClick(PointerEventData eventData)
    {
        
    }

    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {   
        
    }

    // 드래그 진행중
    public void OnDrag(PointerEventData eventData)
    {
        
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        
    }

    // 드랍 이벤트
    public void OnDrop(PointerEventData eventData)
    {
        
    }
}
