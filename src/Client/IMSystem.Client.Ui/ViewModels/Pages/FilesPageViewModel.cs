using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.ViewModels.Pages
{
    public partial class FilesPageViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private FilesListViewModel _filesList;

        [ObservableProperty]
        private FileDetailViewModel _fileDetail;

        public FilesPageViewModel()
        {
            // 构造函数仅创建必要对象
            _filesList = new FilesListViewModel();
            _fileDetail = new FileDetailViewModel();
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            // 监听文件列表选择变化
            _filesList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilesListViewModel.SelectedItem) && _filesList.SelectedItem != null)
                {
                    UpdateFileDetail(_filesList.SelectedItem);
                }
            };

            // 初始化模拟数据
            InitializeMockData();
            
            _isInitialized = true;
        }

        private void UpdateFileDetail(FileItemViewModel file)
        {
            // 更新文件详情
            _fileDetail.FileName = file.FileName;
            _fileDetail.FileSize = file.FileSize;
            _fileDetail.UploadDate = file.UploadDate;
            _fileDetail.FileIcon = file.FileIcon;
            
            // 设置是否有预览
            bool isImageFile = file.FileName.EndsWith(".jpg") || 
                               file.FileName.EndsWith(".png");
            _fileDetail.HasPreview = isImageFile;
            
            // 模拟加载预览图
            if (isImageFile)
            {
                // 实际项目中，这里应该是从文件服务加载预览图
                // 这里仅作为演示使用空白图像
                _fileDetail.PreviewImage = null;
            }
        }

        private void InitializeMockData()
        {
            _filesList.Items.Add(new FileItemViewModel 
            { 
                FileName = "项目计划.docx", 
                FileSize = "128 KB", 
                UploadDate = "2025-05-16",
                FileIcon = SymbolRegular.Document24
            });
            
            _filesList.Items.Add(new FileItemViewModel 
            { 
                FileName = "项目进度报告.xlsx", 
                FileSize = "256 KB", 
                UploadDate = "2025-05-14",
                FileIcon = SymbolRegular.Table24
            });
            
            _filesList.Items.Add(new FileItemViewModel 
            { 
                FileName = "会议记录.pdf", 
                FileSize = "512 KB", 
                UploadDate = "2025-05-12",
                FileIcon = SymbolRegular.DocumentPdf24
            });
            
            _filesList.Items.Add(new FileItemViewModel 
            { 
                FileName = "产品截图.png", 
                FileSize = "1.2 MB", 
                UploadDate = "2025-05-10",
                FileIcon = SymbolRegular.Image24
            });
            
            // 默认选中第一项
            if (_filesList.Items.Count > 0)
            {
                _filesList.SelectedItem = _filesList.Items[0];
            }
        }
    }

    public partial class FilesListViewModel : ObservableObject
    {
        public ObservableCollection<FileItemViewModel> Items { get; } = new();

        [ObservableProperty]
        private FileItemViewModel _selectedItem;
    }

    public partial class FileItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _fileName;

        [ObservableProperty]
        private string _fileSize;

        [ObservableProperty]
        private string _uploadDate;

        [ObservableProperty]
        private SymbolRegular _fileIcon;
    }

    public partial class FileDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _fileName;

        [ObservableProperty]
        private string _fileSize;

        [ObservableProperty]
        private string _uploadDate;

        [ObservableProperty]
        private SymbolRegular _fileIcon;

        [ObservableProperty]
        private bool _hasPreview;

        [ObservableProperty]
        private BitmapImage _previewImage;

        [RelayCommand]
        private void Download()
        {
            // 实现下载功能
        }

        [RelayCommand]
        private void Share()
        {
            // 实现分享功能
        }

        [RelayCommand]
        private void Delete()
        {
            // 实现删除功能
        }
    }
}