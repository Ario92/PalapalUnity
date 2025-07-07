// Assets/Editor/FolderStructureSettings.cs
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// این اتریبیوت به یونیتی اجازه می‌دهد تا یک asset از این نوع در پروژه ایجاد کند.
[CreateAssetMenu(fileName = "FolderStructureSettings", menuName = "Folder Structure Validator/Settings")]
public class FolderStructureSettings : ScriptableObject
{
    // لیستی از فولدرها که می‌خواهیم ساختار آن‌ها را بررسی کنیم.
    // از DefaultAsset استفاده می‌کنیم تا کاربر بتواند فولدر را مستقیم از پروژه بکشد و رها کند.
    [Tooltip("فولدرهایی که می‌خواهید ساختارشان بررسی شود را اینجا اضافه کنید.")]
    public List<DefaultAsset> TargetFolders = new List<DefaultAsset>();

    // یک متغیر برای فعال یا غیرفعال کردن اعتبارسنجی
    [Tooltip("اگر این گزینه فعال باشد، اعتبارسنجی ساختار فولدر انجام می‌شود.")]
    public bool IsValidationEnabled = true;
}

#endif