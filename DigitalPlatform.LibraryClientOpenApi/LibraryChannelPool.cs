﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPlatform.LibraryClientOpenApi
{
    /// <summary>
    /// 通道池
    /// </summary>
    public class LibraryChannelPool : List<ChannelWrapper>
    {
        //允许多个线程同时获取读锁，但同一时间只允许一个线程获得写锁，因此也称作共享-独占锁
        internal ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
        internal static int m_nLockTimeout = 5000;	// 5000=5秒

        /// <summary>
        /// 登录前事件
        /// </summary>
        public event BeforeLoginEventHandle? BeforeLogin;

        /// <summary>
        /// 登录后事件
        /// </summary>
        public event AfterLoginEventHandle? AfterLogin;

        /// <summary>
        /// 最多通道数
        /// </summary>
        //public int MaxCount = 50;

        /// <summary>
        /// 征用一个通道
        /// </summary>
        /// <param name="strUrl">服务器 URL</param>
        /// <param name="strUserName">用户名</param>
        /// <returns>返回通道对象</returns>
        public LibraryChannel GetChannel(string strUrl,
            string strUserName)
        {
            ChannelWrapper? wrapper = null;

            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new LockException("锁定尝试中超时");

            try
            {
                // 先从池中查找存在此url的空闭通道
                wrapper = this.GetChannelInternel(strUrl, strUserName, true);
                if (wrapper != null)
                    return wrapper.Channel;

                ////超出数量，需清理不用的通道
                //if (this.Count >= MaxCount)
                //{
                //    // 清理不用的通道
                //    int nDeleteCount = CleanChannel(false);
                //    if (nDeleteCount == 0)
                //    {
                //        // 全部都在使用
                //        throw new Exception("通道池已满，请稍候重试获取通道");
                //    }
                //}

                // 如果没有找到
                LibraryChannel inner_channel = new LibraryChannel();
                inner_channel.Url = strUrl;
                inner_channel.UserName = strUserName;
                inner_channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);
                inner_channel.AfterLogin += Inner_channel_AfterLogin;

                wrapper = new ChannelWrapper(inner_channel);
                // wrapper.Channel = inner_channel;
                wrapper.InUsing = true;

                this.Add(wrapper);
                return inner_channel;
            }
            finally
            {
                this.m_lock.ExitWriteLock(); //释放锁
            }
        }

        private void Inner_channel_AfterLogin(object sender, AfterLoginEventArgs e)
        {
            if (this.AfterLogin != null)
                this.AfterLogin(sender, e);
        }

        /// <summary>
        /// 触发BeforeLogin事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            if (this.BeforeLogin != null)
                this.BeforeLogin(sender, e);
        }

        // 从池中查找指定URL的LibraryChannel对象
        ChannelWrapper? GetChannelInternel(string strUrl,
            string strUserName,
            bool bAutoSetUsing)
        {
            foreach (ChannelWrapper wrapper in this)
            {
                if (wrapper.InUsing == false
                    && wrapper.Channel.Url == strUrl
                    && wrapper.Channel.UserName == strUserName)
                {
                    if (bAutoSetUsing == true)
                        wrapper.InUsing = true;
                    return wrapper;
                }
            }

            return null;
        }

        // 查找指定URL的LibraryChannel对象
        ChannelWrapper? GetChannelInternel(LibraryChannel inner_channel)
        {
            foreach (ChannelWrapper channel in this)
            {
                if (channel.Channel == inner_channel)
                {
                    return channel;
                }
            }

            return null;
        }

        /// <summary>
        /// 归还一个通道
        /// </summary>
        /// <param name="channel">通道对象</param>
        public void ReturnChannel(LibraryChannel channel)
        {
            ChannelWrapper? wrapper = null;
            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new LockException("锁定尝试中超时");
            try
            {
                wrapper = GetChannelInternel(channel);
                if (wrapper != null)
                    wrapper.InUsing = false;
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }
        }

        public int CleanChannel()
        {
            return this.CleanChannel(true);
        }

        // 清理不用的通道
        // return:
        //      清理掉的通道数目
        public int CleanChannel(bool bLock)
        {
            // 要需清理的放到内存里
            List<ChannelWrapper> deletes = new List<ChannelWrapper>();

            if (bLock == true)
            {
                if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                    throw new LockException("锁定尝试中超时");
            }

            try
            {
                for (int i = 0; i < this.Count; i++)
                {
                    ChannelWrapper wrapper = this[i];
                    if (wrapper.InUsing == false)
                    {
                        this.RemoveAt(i);
                        i--;
                        deletes.Add(wrapper);
                    }
                }
            }
            finally
            {
                if (bLock == true)
                    this.m_lock.ExitWriteLock();
            }

            // 关闭这些通道
            foreach (ChannelWrapper wrapper in deletes)
            {
                wrapper.Channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
                wrapper.Channel.AfterLogin -= Inner_channel_AfterLogin;
                wrapper.Channel.Close();
            }

            return deletes.Count;
        }

        /// <summary>
        /// 关闭所有通道，清除集合
        /// </summary>
        public void Close()
        {
            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new LockException("锁定尝试中超时");
            try
            {
                foreach (ChannelWrapper wrapper in this)
                {
                    wrapper.Channel.Close();
                }

                this.Clear();
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// 通道包装对象
    /// </summary>
    public class ChannelWrapper
    {
        /// <summary>
        /// 通道是否正在使用中
        /// </summary>
        public bool InUsing = false;

        /// <summary>
        /// 通道对象
        /// </summary>
        public LibraryChannel Channel;

        public ChannelWrapper(LibraryChannel channel)
        {
            Channel = channel;
        }
    }

    public class LockException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="error"></param>
        /// <param name="strText"></param>
        public LockException(string strText)
            : base(strText)
        {
        }
    }

}
