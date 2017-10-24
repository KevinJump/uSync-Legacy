using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Jumoo.uSync.Audit.Persistance.Model;

namespace Jumoo.uSync.Audit.Persistance.Mappers
{
    internal class ModelMappings
    {
        internal ModelMappings()
        {

        }

        internal void Initialzie()
        {
            // todo - we need a xml to list mapper both ways.

            Mapper.CreateMap<uSyncChangeGroup, uSyncChangeGroupDTO>();
            Mapper.CreateMap<uSyncChangeGroupDTO, uSyncChangeGroup>();

            Mapper.CreateMap<uSyncItemChanges, uSyncItemChangesDTO>()
                .ForMember(
                    dest => dest.Changes,
                    opt => opt.ResolveUsing<uSyncChangeListResolver>()
                        .FromMember(src => src.Changes)
                );

            Mapper.CreateMap<uSyncItemChangesDTO, uSyncItemChanges>()
                .ForMember(
                    dest => dest.Changes,
                    opt => opt.ResolveUsing<uSyncChangeListFromStringResolver>()
                        .FromMember(src => src.Changes)
                );


        }
    }
}
