   M S B A M L   `   `   `  ����O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem  5      I EFDUserControls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullG4clr-namespace:FDUserControls;assembly=FDUserControlsFDUserControls 5       EDLaunch   Launcher.SettingsPage   5
      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      6sys&clr-namespace:System;assembly=mscorlib     5      Efduc4clr-namespace:FDUserControls;assembly=FDUserControls     5        N� Title$  SettingsPage��5       � 	Resources 5         
ChoicesCol  SeparatorCol  SeparatorRow  SeparatorColWidth  
DynamicCol  
DynamicRow��%�   &	        &	      &	 2     &	 K     &	 e     &	 ~     ��5   
   1��6*   ��5   
   1��6,   ��5   
   1��6,   �5   
   10 �63   ��5   
   1��6*   ��5    
   1��6*   5!      .��5$      �  � ColumnDefinitions 5&   
   �� 5'      $��* �6   6+   �� 5(      #��[ 6   6L   �� 5)      -   5         ��Name$ DynamicSettingsColumn��5)      $��1.25* �6>   6N    � RowDefinitions 5,   
   �� 5-      $��80 �6   6*   �� 5.      $��* �6   6)   �� 5/      $��60 �6   6*   .+�53   
   � $��0��6   $��1��6    54      �� 55      $��* �6#   6/   �� 56      $��2* �6#   60   �� 57      $��* �6#   6/   .+�5;      !  FDUserControls.SettingsCtrl  -   5          Name$ SettingsCtrl��5;       #��[  5<       5=      5>      �� 5A   
   #��[ 6   #��[ 5B      $��Center=�5C       � LayoutTransform 5D      �� 5E       �� Angle$ 90Q�6"   5G      � 5J   
   $��2��6   $��1��6    5K      �� 5L      $��* �6#   6/   �� 5M      $��8* �6#   60   �� 5N      $��* �6#   6/   .+�5Q       � -   5          �Name$ DynSettingsFrame��5Q      #��[ 5R      #��[ 5S      	  � NavigationUIVisibility$	 Hidden=�5T      5U      5V      5W      5Y      