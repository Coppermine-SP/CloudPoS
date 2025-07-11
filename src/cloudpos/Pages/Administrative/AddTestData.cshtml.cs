using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Pages.Customer;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Order = CloudInteractive.CloudPos.Models.Order;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public class AddTestData(ILogger<Authorize> logger, ServerDbContext context) : PageModel
{
    public void OnGet()
    {
        /*
         * 생활맥주 수제맥주는 맛이 좋습니다.
         * 데모 데이터로 생활맥주 / (주)데일리비어의 메뉴 자료를 사용하였습니다.
         */
        
        //Categories
        var category1 = new Category()
        {
            Name = "메인"
        };
        var category2 = new Category()
        {
            Name = "사이드"
        };
        var category3 = new Category()
        {
            Name = "수제맥주"
        };
        var category4 = new Category()
        {
            Name = "하이볼"
        };
        context.Categories.Add(category1);
        context.Categories.Add(category2);
        context.Categories.Add(category3);
        context.Categories.Add(category4);
        
        //Tables
        var table1 = new Table()
        {
            Name = "1 (창가)"
        };
        var table2 = new Table()
        {
            Name = "2 (창가)"
        };
        var table3 = new Table()
        {
            Name = "3 (창가)"
        };
        var table4 = new Table()
        {
            Name = "4 (창가)"
        };
        var table5 = new Table()
        {
            Name = "5 (복도)"
        };
        var table6 = new Table()
        {
            Name = "6 (복도)"
        };
        var table7 = new Table()
        {
            Name = "7 (복도)"
        };
        var table8 = new Table()
        {
            Name = "8 (복도)"
        };
        var table9 = new Table()
        {
            Name = "9 (복도)"
        };
        var table10 = new Table()
        {
            Name = "10 (룸)"
        };
        var table11 = new Table()
        {
            Name = "11 (룸)"
        };
        var table12 = new Table()
        {
            Name = "12 (룸)"
        };
        context.Tables.Add(table1);
        context.Tables.Add(table2);
        context.Tables.Add(table3);
        context.Tables.Add(table4);
        context.Tables.Add(table5);
        context.Tables.Add(table6);
        context.Tables.Add(table7);
        context.Tables.Add(table8);
        context.Tables.Add(table9);
        context.Tables.Add(table10);
        context.Tables.Add(table11);
        context.Tables.Add(table12);
        
        //Items
        Item[] items =
        {
        /* ---- 수제맥주 ----- */
        new() { Name = "KCBC 청사맥주", Description = "한국크래프트맥주클럽 KCBC의 11개 양조장들이 각기 다른 아로마 계열의 싱글 홉을 사용해 매달 다른 풍미와 매력을 느낄 수 있는 맥주", Category = category3, IsAvailable = true, ImageId = 1, Price = 6500 },
        new() { Name = "시나몬달콤다크", Description = "달콤한 시나몬슈가와 찰떡궁합으로 은은한 초코와 커피의 풍미가 매력적인 다크라거", Category = category3, IsAvailable = true, ImageId = 2, Price = 7000 },
        new() { Name = "KCBC 청룡맥주", Description = "지역 양조장별 다른 물의 미네랄과 상큼한 과일향, 부드러운 목넘김이 매력적인 한국인의 입맛에 맞춘 한국식 페일에일!", Category = category3, IsAvailable = true, ImageId = 3, Price = 7000 },
        new() { Name = "고양톡톡에일", Description = "낮은 온도에서 발효해 라거 같은 깔끔함과 홉의 향긋함을 함께 즐길 수 있는 골든에일", Category = category3, IsAvailable = true, ImageId = 4, Price = 6500 },
        new() { Name = "군포수리에일", Description = "몰트의 고소함과 향긋한 홉향의 밸런스가 잘 갖춰져 언제 어디서나 가볍게 즐길 수 있는 맥주", Category = category3, IsAvailable = true, ImageId = 5, Price = 6500 },
        new() { Name = "가평클래식필스너", Description = "맥아의 진한 풍미에 쌉싸름하면서도 고급스러운 아로마와 라거의 청량감을 모두 갖춘 필스너", Category = category3, IsAvailable = true, ImageId = 6, Price = 6500 },
        new() { Name = "용인살구IPA", Description = "살구의 은은하고 싱그러운 향과 4종 이상의 홉이 만들어 내는 풍성한 쥬시함이 특징인 IPA", Category = category3, IsAvailable = true, ImageId = 7, Price = 7500 },
        new() { Name = "가평청춘라거", Description = "은은한 꽃향과 싱그러운 과일향을 느낄 수 있는 청량한 라거", Category = category3, IsAvailable = true, ImageId = 8, Price = 6500 },
        new() { Name = "금강 IPA", Description = "풍부하지만 과하지 않은 홉의 꽃향과 과일향, 은은한 단맛이 좋은 여운을 남기는 정통 미국식 IPA", Category = category3, IsAvailable = true, ImageId = 9, Price = 7500 },
        new() { Name = "부산초코스타우트", Description = "진짜 초콜릿을 듬뿍 넣어 묵직한 목넘김을 자랑하는 흑맥주", Category = category3, IsAvailable = true, ImageId = 10, Price = 7000 },
        new() { Name = "제주까망포터", Description = "커피를 연상시키는 로스티한 뉘앙스와 부드러운 끝 맛이 어우러진 흑맥주", Category = category3, IsAvailable = true, ImageId = 11, Price = 7000 },
        new() { Name = "파인애플 골든에일", Description = "파인애플을 듬뿍 넣어 달콤한 과일향과 시트러스함이 조화로운 골든에일", Category = category3, IsAvailable = true, ImageId = 12, Price = 7000 },
        new() { Name = "생활기본맥주", Description = "생활맥주 8주년 기념 페일에일로 깔끔한 쓴맛과 상쾌한 과일향이 특징", Category = category3, IsAvailable = true, ImageId = 13, Price = 6500 },

        /* ---- 하이볼 ----- */
        new() { Name = "리얼레몬 하이볼", Description = "톡 쏘는 레몬의 상큼·달달함과 청량한 탄산의 조화가 매력적인 레몬 하이볼", Category = category4, IsAvailable = true, ImageId = 14, Price = 8000 },
        new() { Name = "얼그레이 하이볼", Description = "진하고 달콤한 얼그레이 향이 가득해 시원하고 청량하게 즐기는 생(生) 하이볼", Category = category4, IsAvailable = true, ImageId = 15, Price = 8000 },
        new() { Name = "골드하이볼", Description = "향긋한 베르가못 향과 아메리칸 위스키 원액이 만들어 내는 풍부한 위스키 풍미", Category = category4, IsAvailable = true, ImageId = 16, Price = 8500 },
        new() { Name = "레드하이볼", Description = "향긋한 유자향과 은은한 위스키·히비스커스의 달콤함이 어우러진 하이볼", Category = category4, IsAvailable = true, ImageId = 17, Price = 8500 },

        /* ---- 메인 안주 ----- */
        new() { Name = "바삭 유부 김밥", Description = "유부가 듬뿍 들어간 바삭 김밥 튀김", Category = category1, IsAvailable = true, ImageId = 18, Price = 16000 },
        new() { Name = "생활 부대 떡볶이", Description = "부대떡볶이의 생명은 뭐다?! 소시지다!", Category = category1, IsAvailable = true, ImageId = 19, Price = 18000 },
        new() { Name = "크리스피 텐더와 새우", Description = "크리스피 텐더와 감자가 껍질째 먹는 통새우와 함께 돌아왔어요.", Category = category1, IsAvailable = true, ImageId = 20, Price = 18000 },
        new() { Name = "앵그리버드 크런치", Description = "양파·마늘·콘후레이크·검은깨로 풍미와 바삭함이 가득!", Category = category1, IsAvailable = true, ImageId = 21, Price = 17000 },
        new() { Name = "앵그리버드 RED", Description = "베트남 땡초 고추와 마늘로 매콤한 맛을 낸 스페셜 양념치킨", Category = category1, IsAvailable = true, ImageId = 22, Price = 17000 },
        new() { Name = "앵그리버드 BLACK", Description = "숙성 간장·마늘·볶은 참깨가 어우러진 단짠 간장치킨", Category = category1, IsAvailable = true, ImageId = 23, Price = 17000 },
        new() { Name = "골빔면", Description = "골뱅이 한 통과 매콤새콤 특제 소스가 들어간 비빔면", Category = category1, IsAvailable = true, ImageId = 24, Price = 16000 },
        new() { Name = "버팔로 윙과 감자", Description = "계속 손이 가는 매콤 달콤 버팔로 윙", Category = category1, IsAvailable = true, ImageId = 25, Price = 17000 },

        /* ---- 사이드 안주 ----- */
        new() { Name = "케이준 감자튀김", Description = "짭짤한 케이준 양념 감자튀김", Category = category2, IsAvailable = true, ImageId = 26, Price = 9000 },
        new() { Name = "오리지널 감자튀김", Description = "기본 중의 기본 감자튀김", Category = category2, IsAvailable = true, ImageId = 27, Price = 8000 },
        new() { Name = "통통새우바", Description = "역시 탱글한 건 통새우지! 통새우 7마리를 그대로~", Category = category2, IsAvailable = true, ImageId = 28, Price = 10000 },
        new() { Name = "쫜득 치즈볼", Description = "쫜득쫜득 치즈 듬뿍 치즈볼", Category = category2, IsAvailable = true, ImageId = 29, Price = 9000 },
        new() { Name = "팝만두", Description = "맥주도둑! 바삭바삭 크리스피 팝만두", Category = category2, IsAvailable = true, ImageId = 30, Price = 8000 },
        new() { Name = "우유튀김", Description = "우유를 튀긴다고? 겉은 바삭, 속은 촉촉한 우유튀김", Category = category2, IsAvailable = true, ImageId = 31, Price = 8000 },
        new() { Name = "스팸튀김", Description = "진짜 스팸을 통째로 튀겼습니다.", Category = category2, IsAvailable = true, ImageId = 32, Price = 9000 },
        new() { Name = "나초칩", Description = "나초와 살사·치즈소스", Category = category2, IsAvailable = true, ImageId = 33, Price = 8000 },
        new() { Name = "번데기 치즈 뇨끼", Description = "쫄깃한 뇨끼를 곁들인 번데기 스튜!", Category = category2, IsAvailable = true, ImageId = 34, Price = 11000 },
        new() { Name = "바삭 황태 구이", Description = "마른 안주계의 황태자! 바삭한 황태 구이에 짭짤한 시즈닝 가루", Category = category2, IsAvailable = true, ImageId = 35, Price = 12000 }
        };
        context.Items.AddRange(items);
        
        //Sample Session
        context.Sessions.Add(new TableSession()
        {
            Table = table11,
            AuthCode = "MSFT",
            CreatedAt = DateTime.Now
        });

        context.SaveChanges();
    }
}