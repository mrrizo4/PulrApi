using System.Linq;
using Core.Application.Mediatr.MediaFiles.Commands;
using Core.Application.Models.MediaFiles;
using Core.Domain.Entities;

namespace Core.Application.Mappings
{
    public class MediaFileProfile : AutoMapper.Profile
    {
        public MediaFileProfile()
        {
            CreateMap<UploadMediaFileDto, UploadMediaFileCommand>();

            CreateMap<MediaFile, MediaFileDetailsResponse>()
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.MediaFileType.ToString()));
        }
    }
}
