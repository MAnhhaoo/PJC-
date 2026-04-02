using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TourismApp.Services
{
    public class LanguageService : INotifyPropertyChanged
    {
        private string _currentLanguage = "vi";

        // Hiện có sẵn tên hiển thị -> mã ngôn ngữ
        public Dictionary<string, string> AvailableLanguages { get; } = new()
        {
            { "Tiếng Việt", "vi" },
            { "English", "en" },
            { "Japanese", "ja" },
            { "French", "fr" },
            { "Korean", "ko" },
            { "Chinese", "zh" }
        };

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value.ToLower();
                    // Phát sự kiện PropertyChanged với chuỗi rỗng để cập nhật TẤT CẢ các Binding sử dụng indexer []
                    OnPropertyChanged(string.Empty);
                    OnPropertyChanged(nameof(CurrentLanguage));
                }
            }
        }

        public LanguageService()
        {
            try
            {
                // Nếu người dùng đã lưu lựa chọn trước đó => load lên
                var saved = Microsoft.Maui.Storage.Preferences.Default.Get("UserLanguage", string.Empty);
                if (!string.IsNullOrEmpty(saved))
                    _currentLanguage = saved.ToLower();
            }
            catch { }
        }

        // Indexer: Giúp XAML gọi được dạng {Binding LangService[HomeTitle]}
        public string this[string key] => GetText(key);

        public string GetText(string key)
        {
            var translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["vi"] = new Dictionary<string, string> {
                    {"HomeTitle", "Khu phố ẩm thực Vĩnh Khánh"},
                    {"SubTitle", "Thiên đường ốc và hải sản nổi tiếng Quận 4, sôi động về đêm."},
                    {"Listen", "Nghe thuyết minh"},
                    {"Stop", "Dừng"},
                    {"Map", "Xem bản đồ"},
                    {"Profile", "Hồ sơ cá nhân"},
                    {"SelectLang", "Chọn ngôn ngữ"},
                    {"Cancel", "Hủy"},
                    {"NoNarration", "Nhà hàng này hiện chưa có bài thuyết minh nào."}
                },
                ["en"] = new Dictionary<string, string> {
                    {"HomeTitle", "Vinh Khanh Food Street"},
                    {"SubTitle", "Famous snail and seafood paradise in District 4."},
                    {"Listen", "Play Audio"},
                    {"Stop", "Stop"},
                    {"Map", "View Map"},
                    {"Profile", "My Profile"},
                    {"SelectLang", "Select Language"},
                    {"Cancel", "Cancel"},
                    {"NoNarration", "This restaurant currently has no narration."}
                },
                ["ja"] = new Dictionary<string, string> {
                    {"HomeTitle", "ビンカン食道街"},
                    {"SubTitle", "第4区で有名なカタツムリとシーフードのパラダイス。"},
                    {"Listen", "音声再生"},
                    {"Stop", "停止"},
                    {"Map", "地図を見る"},
                    {"Profile", "プロフィール"},
                    {"SelectLang", "言語を選択"},
                    {"Cancel", "キャンセル"},
                    {"NoNarration", "このレストランには現在ナレーションがありません。"}
                },
                ["fr"] = new Dictionary<string, string> {
                    {"HomeTitle", "Rue culinaire de Vinh Khanh"},
                    {"SubTitle", "Célèbre paradis des escargots et fruits de mer au District 4."},
                    {"Listen", "Écouter"},
                    {"Stop", "Arrêter"},
                    {"Map", "Voir la carte"},
                    {"Profile", "Mon profil"},
                    {"SelectLang", "Choisir la langue"},
                    {"Cancel", "Annuler"},
                    {"NoNarration", "Ce restaurant n'a pas encore de narration."}
                },
                ["ko"] = new Dictionary<string, string> {
                    {"HomeTitle", "빈칸 음식 거리"},
                    {"SubTitle", "4구의 유명한 달팽이와 해산물 낙원."},
                    {"Listen", "오디오 듣기"},
                    {"Stop", "중지"},
                    {"Map", "지도 보기"},
                    {"Profile", "내 프로필"},
                    {"SelectLang", "언어 선택"},
                    {"Cancel", "취소"},
                    {"NoNarration", "이 레스토랑은 현재 설명이 없습니다."}
                },
                ["zh"] = new Dictionary<string, string> {
                    {"HomeTitle", "永庆美食街"},
                    {"SubTitle", "第四区著名的螺类和海鲜天堂。"},
                    {"Listen", "播放音频"},
                    {"Stop", "停止"},
                    {"Map", "查看地图"},
                    {"Profile", "个人资料"},
                    {"SelectLang", "选择语言"},
                    {"Cancel", "取消"},
                    {"NoNarration", "该餐厅目前没有解说。"}
                }
            };

            if (translations.ContainsKey(CurrentLanguage) && translations[CurrentLanguage].ContainsKey(key))
            {
                return translations[CurrentLanguage][key];
            }

            // Nếu không tìm thấy, mặc định trả về tiếng Anh để không bị trống giao diện
            if (translations["en"].ContainsKey(key))
            {
                return translations["en"][key];
            }

            return key;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}