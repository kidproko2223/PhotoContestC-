﻿namespace PhotoContest.Infrastructure.Services
{
    using PhotoContest.Data.Interfaces;

    public class BaseService
    {
        private IPhotoContestData _data;

        protected BaseService(IPhotoContestData data)
        {
            this.Data = data;
        }

        protected IPhotoContestData Data
        {
            get
            {
                return this._data;
            }
            set
            {
                this._data = value;
            }
        }
    }
}