using UnityEngine;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// Addressable IAssetLoader 설정 가이드.
    ///
    /// UnityScreenNavigator는 기본적으로 ResourcesAssetLoader를 사용.
    /// Addressables로 교체하려면:
    ///
    /// 1. Assets > Create > Screen Navigator > Addressable Asset Loader
    /// 2. 생성된 AddressableAssetLoaderObject를 선택
    /// 3. UnityScreenNavigatorSettings에서 Default Asset Loader에 할당
    ///    또는 각 Container의 Asset Loader 필드에 개별 할당
    ///
    /// 프리팹을 Addressable로 마킹:
    /// 1. Page/Modal/Sheet 프리팹 선택
    /// 2. Inspector > Addressable 체크
    /// 3. Address 키를 Push()의 resourceKey로 사용
    ///
    /// Container별 개별 설정:
    /// PageContainer, ModalContainer, SheetContainer 각각
    /// Inspector에서 다른 AssetLoader를 지정 가능.
    /// 예: Pages는 Addressables, Modals는 Resources 혼용
    /// </summary>
    public static class AddressableAssetLoaderSetup
    {
        /// <summary>
        /// 런타임에서 AssetLoader 교체가 필요한 경우 참고.
        /// 일반적으로는 Inspector에서 설정하는 것이 권장됨.
        /// </summary>
        public static void LogSetupInstructions()
        {
            Debug.Log(@"[AddressableAssetLoader 설정 가이드]
1. Assets > Create > Screen Navigator > Addressable Asset Loader
2. UnityScreenNavigatorSettings.AssetLoader에 할당
3. 프리팹을 Addressable로 마킹
4. Push(addressableKey) 로 사용");
        }
    }
}
