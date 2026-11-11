using UnityEngine;
using System.Collections;


// 商店物品类型
public enum ShopItemType
{

    Potion,         // 回满生命
    KnifeGrinder    // 攻击伤害 +1

}



// 商店触发箱：玩家碰到时在箱子上方显示价格 "x金币"，
// 玩家按 E 购买（可重复购买），购买后触发对应效果。
public class ShopController : MonoBehaviour
{

    public ShopItemType itemType;

    public int price = 1;

    // 价格文字相对触发箱的位置，可在 Inspector 里直接调整（X 左右，Y 上下）
    public Vector3 labelOffset = new Vector3(0f, 1.2f, 0f);

    // 价格文字大小倍率，可在 Inspector 调整（1=当前大小，2=两倍大，0.5=一半）
    public float labelSize = 1f;

    // 价格文字内容：留空则自动显示"价格 coin"，填了就以这里输入的文字为准
    public string labelText = "";

    // 可选：在场景里手动放置一个 TextMesh（名字随意，建议叫 PriceLabel）拖到这里，
    // 就能直接在场景里拖动调整位置、改字号调大小，保存即生效。留空则用上面的字段自动生成。
    public TextMesh priceLabel;

    private bool playerInside = false;



    void Start()
    {

        CreatePriceLabel();

        playerInside = false;

    }



    void Update()
    {

        if(playerInside && Input.GetKeyDown(KeyCode.E))
            TryBuy();

    }

    // 返回价格标签要显示的文字：labelText 填了就用它，否则自动拼"价格 coin"
    string GetLabelText()
    {
        return string.IsNullOrEmpty(labelText) ? price + " coin" : labelText;
    }



    // 在触发箱上方创建价格标签（默认隐藏）
    void CreatePriceLabel()
    {

        // 优先使用场景里手动放置的 PriceLabel（TextMesh）：
        // 可直接在场景里拖动调整位置、改字号调大小，保存即生效。
        if(priceLabel != null)
        {
            priceLabel.font = CoinManager.LoadUIFont();
            priceLabel.text = GetLabelText();
            priceLabel.gameObject.SetActive(false);
            return;
        }

        // 否则运行时自动创建
        GameObject labelGO = new GameObject("PriceLabel");
        labelGO.transform.SetParent(transform, false);
        labelGO.transform.localPosition = labelOffset;

        priceLabel = labelGO.AddComponent<TextMesh>();
        // 用 LoadUIFont()：它内部会先 RequestCharactersInTexture 再校验"币"字形，
        // 保证返回的字体确实能渲染中文。
        // 不要用 CreateDynamicFontFromOSFont()：系统找不到字体时也会返回"空字体"，
        // 直接用它渲染"金币"会变成乱码/方框。
        priceLabel.font = CoinManager.LoadUIFont();
        // fontSize 固定保证清晰，characterSize 由 labelSize 倍率控制显示大小。
        priceLabel.fontSize = 2048;
        priceLabel.characterSize = 0.01f * labelSize;
        priceLabel.anchor = TextAnchor.MiddleCenter;
        priceLabel.alignment = TextAlignment.Center;
        priceLabel.color = Color.yellow;
        priceLabel.text = GetLabelText();

        // 不要手动覆盖材质：TextMesh 会自动从字体挂载材质，
        // 手动赋值可能用上低分辨率图集导致文字变糊。
        labelGO.SetActive(false);

    }



    void OnTriggerEnter2D(Collider2D other)
    {

        if(!other.CompareTag("Player"))
            return;

        playerInside = true;

        if(priceLabel != null)
        {

            priceLabel.text = GetLabelText();
            priceLabel.gameObject.SetActive(true);

        }

    }



    void OnTriggerExit2D(Collider2D other)
    {

        if(!other.CompareTag("Player"))
            return;

        playerInside = false;

        if(priceLabel != null)
            priceLabel.gameObject.SetActive(false);

    }



    void TryBuy()
    {

        if(CoinManager.TrySpend(price))
        {
            ApplyEffect();
        }
        else
        {

            // 金币不足，临时提示
            StartCoroutine(ShowNotEnough());

        }

    }



    IEnumerator ShowNotEnough()
    {

        if(priceLabel != null)
            priceLabel.text = "coins not enough";

        yield return new WaitForSeconds(1f);

        if(priceLabel != null && playerInside)
            priceLabel.text = GetLabelText();

    }



    // 应用购买效果
    void ApplyEffect()
    {

        GameObject player = GameObject.FindWithTag("Player");
        if(player == null)
            return;

        if(itemType == ShopItemType.Potion)
        {

            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if(ph != null)
                ph.HealFull();

        }
        else if(itemType == ShopItemType.KnifeGrinder)
        {

            // 找到玩家实际使用的剑（weaponSprite），永久提升其伤害
            PlayerAttack pa = player.GetComponent<PlayerAttack>();
            if(pa != null && pa.weaponSprite != null)
            {

                WeaponAttack wa = pa.weaponSprite.GetComponent<WeaponAttack>();
                if(wa != null)
                    wa.damage += 1;

            }

        }

    }

}