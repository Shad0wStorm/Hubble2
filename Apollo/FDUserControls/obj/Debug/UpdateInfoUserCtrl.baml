   M S B A M L   `   `   `  ����O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem  5       FDUserControls'   !FDUserControls.UpdateInfoUserCtrl   5
      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      6sys&clr-namespace:System;assembly=mscorlib     5        � 	Resources  5         VerticalBorder  InternalVerticalBorder  MainDisplayAreaHeight  CtrlBackground  ProductNameForeground  ProductNameFontSize  VersionFontSize  HyperlinkFontSize��%/  &	        &	      &	 3     &	 M     &	 p     &	 �     &	 �     &	 �     �5   
   15 �60   �5   
   5 �67   �5   
   95 �67   ��5   
   Transparent��6>   ��5   
   	LightGray��6C   X�5!   
   20Q�65   X�5"   
   16Q�61   X�5#   
   16Q�63   5%      .��5'      � 
������  � RowDefinitions 5)   
   �� 5*      #��[  6   6G   �� 5+      #��[ 6   6N   �� 5,      #��[  6   6G    � ColumnDefinitions 5/   
   �� 50      $��* �6   6+   �� 51      $	��20* �6   6-   �� 52      $��* �6   6+   .+�55   
   � $��1��6   $��1��56      $��Center=�57       59      �� 5:      #��[ 6    6S   �� 5;      $
��auto �6    60   �� 5<      $
��auto �6    60   �� 5=      $
��auto �6    60   �� 5>      #��[ 6    6S   .+�5B      �� -   5         ��Name$ PART_ProductName��5B      $��1��5C      #��[ 5D      #��[ 5E      $��Center=�5F      $��Center=�5G      5H      �� 5K      -   5        $ PART_UpdateInfo��5K      $��2��5L      #��[ 5M        
TitleLabel#��[ 5N      $��Center=�5O      $��Center=�5P      5Q      �� 5T      $��3��6   #��[ 5U       �� TextAlignment$ Center=�5V      $��Center=�5W      �� 5Y      -   5        $3��-https://api.zaonce.net/this/url/never/exists/?�5Z       �� TextDecorations$  {�5[      .�6/   .�5\      �� -   5         ��Name$ PART_HyperLink��5\      64   5]      5_      5a      5d      5e      