   M S B A M L   `   `   `  ����L  HLocalResources, Version=1.7.6938.1, Culture=neutral, PublicKeyToken=null]?clr-namespace:LocalResources.Properties;assembly=LocalResourcesLocalResources.Properties  5      I ECBViewModel, Version=1.7.6938.1, Culture=neutral, PublicKeyToken=null>.clr-namespace:CBViewModel;assembly=CBViewModelCBViewModel 5       EDLaunch   CobraBay.LoginWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Presx?clr-namespace:LocalResources.Properties;assembly=LocalResources     5      >cbv.clr-namespace:CBViewModel;assembly=CBViewModel     5      -   5          � Title$  LoginWindow��5      $	��440��5      $	��440��5       � 
ResizeMode$ NoResize=�5	       � ShowInTaskbar .   � WindowStyle$
 None=�5      ���#1A1A1A � 	Resources 5       �� MergedDictionaries �� Source�� 5   
    5      �� 5      $ Styles/Elite/Shiny.xaml?�6%   %    5      .��5      � $��10q�6    � ColumnDefinitions 5   
   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   .+�5    
   �� -   5        	 ��Name$	 FrontierLogo��5       $��0��6&   $��0��63   $
��None=�6C   $@��:/EDLaunch;component/EliteImages/frontier_logo_grey_sml.png��5!      $
��Left=�6T   �� 5"   
   -   5           CloseButtonStyle#��[  5"      $��0��6K   $��Right=�6[   $��0��5#      $	��Top=�6   � 5%   
   $��20,20,20,15q�6   $��1��6$   ���#1A1A1A 5&      �� 5'      $��* �6#    5)      �� 5*      $
��Auto �6    �� 5+      $
��Auto �6    �� 5,      $
��Auto �6    �� 5-      $��* �6    .+�50      �� $��0��6   $��0 0 0 1q�6x   ��6"   ��   CBViewModel.CBTheme
   Theme �� Source# Z
  �� Path$ DefaultButtonColour�6x   �� 51      -   5         ��Name$ DialogTitle��51      $��WrapWithOverflow=�52      
�������$��0,0,0,3q�53   $   )   #LocalResources.Properties.Resources   DialogTitle#��Z 54      $*��$/EDlaunch;component/Fonts/#Euro Caps)�55      $
��Bold"�6C   $��24(�6U   � 57      -   5         �Name$ 
LoginPanel��57       $��3��6   ���#1A1A1A$y�Visible=�6I    58      �� 59      $��* �6'    5;      �� 5<      $
��Auto �6$   �� 5=      $
��Auto �6$   �� 5>      $��* �6$   �� 5?      $
��Auto �6$   �� 5@      $
��Auto �6$   �� 5A      $
��Auto �6$   �� 5B      $
��Auto �6$   �� 5C      $
��Auto �6$   .+�5F      �� $��1��6   ���#555555$��1 1 1 1q�6<   �� 5G      $��0��6   $,��&Please login to your Frontier Account.��6)    �� TextAlignment$
 Left=�5H      $
��Wrap=�5I      
�������$��	0,13,0,10q�5K      $(��"/EDlaunch;component/Fonts/#Sintony)�5L      $��15(�5M      �� 5N      $��1��6   $	��Top=�6%   $
��Left=�6=   $��10,13,10,10q�5O      $/��)/EDlaunch;component/EliteImages/i-con.png��5P      $
��None=�6G   �� 5Q      $��1��6   $
 Left=�5R      $
��Wrap=�5S      
�������$��35,10,10,10q�5U      $(��"/EDlaunch;component/Fonts/#Sintony)�5V      $��14(�5W      �� 5X      $3�-If you do not have an account already, please��6    �� 5Y      -   5         ��Name$ LinkHelp��5Y       ��62   �� # Z
 $ DefaultButtonColour�.�6�   .�5Z      �� -   5         ��Name$ AccountLinkText��5Z      $�Create New Account��67   5[       �� 5\      $�with Frontier.��6   5]      �� 5_      -   5         ��Name$ StatusBorder��5`      $��1��5_      $��2q�5a      
���  ��y�5c      �� *
Visibility+ �� ElementName$ StatusLabel��._�5d      �� -	   5        $ StatusLabel��5d       $ Center=�5e      $
��Wrap=�6/   $(��"/EDlaunch;component/Fonts/#Sintony)�5f      ���#1A1A1A��5h      �� 5i       �� 
TargetType" ��6$    �� Triggers 5j   "   P� 5k   &    P� Property � �� P� Value$  ��6>   . �5l   *   ��  �� Property � y� �� Value$ Hidden=�6G   5m   '   �� 5n   &    �� Binding 62   �� $ StatusAsError� �� Value	 false6Y   .a�5o   *   ��  � ��$ 12(�6E   �� 5p   *    � ��
 �����6G   5q   '   �� 5r   &    62   �� $ StatusAsError� true6Y   .a�5s   *   ��  � ��$ 20(�6E   �� 5t   *    � ��
 �  ��6G   5u   '   5w      5y      ��5z      �� 5{      " ��6     5|      �� 5}   "    6.   �� $ StatusAsError�	 false6U   .a�5~   &   ��  � ��$ 0q�6H   5   #   �� 5�   "    6.   �� $ StatusAsError� true6U   .a�5�   &   ��  � ��$ 3q�6H   5�   #   5�      5�      �� 5�      $��3��6   $��EMAIL��6)   $
 Left=�5�      $
��Wrap=�5�      
�������$��0,10,0,2q�5�      $*��$/EDlaunch;component/Fonts/#Euro Caps)�5�      $
��Bold"�5�      $��18(�5�      �� 5�      -
   5         ��Name$ UserNameEdit��5�   '   $��4��6   $��0q�5�      $��15(�5�      
���   �$(��"/EDlaunch;component/Fonts/#Sintony)�5�      $��0,5,0,5q�5�      
�������6(   �� 5�      $��5��6   $��PASSWORD��6)   $
 Left=�5�      $
��Wrap=�5�      
�������$��0,10,0,2q�5�      $*��$/EDlaunch;component/Fonts/#Euro Caps)�5�      $
��Bold"�5�      $��18(�5�      G� 5�      -   5         G�Name$ PasswordEdit��5�      $��6��64   $��0q�5�      $��15(�5�      
���   �$(��"/EDlaunch;component/Fonts/#Sintony)�5�      $��0,5,0,5q�5�      
�������6'   � 5�      $��7��6   $��0,20,0,0q�6$    5�      �� 5�      $
��Auto �6+   �� 5�      $��* �6+   �� 5�      $
��Auto �6+   .+�5�      �� -   5         ��Name$ RememberCheck��5�      $��0��5�      $��0��6(   
�������$(��"/EDlaunch;component/Fonts/#Sintony)�5�      $��16(�5�      $
��Left=�5�          IMGT_RememberMe#��Z  5�      ��5�      �� 5�      ! �� 
TargetType"! ��6.   .R�5�   "   �� " �� Orientation$" 
Horizontal=�6-   .+�5�   &   �� $��20��6,   $��20��67   ��5�   *   �� 5�   .   .�5�   2   ��  � ��$4 ./EDLaunch;component/EliteImages/a1Checkbox.png��6Q    5�   2   �� 5�   6    6B   �� *	IsChecked+# �� RelativeSource# �� $ �� AncestorType"$ �� True6�   .a�5�   :   ��  � ��$: 4/EDLaunch;component/EliteImages/a1CheckboxActive.png��6Y   5�   7   �� 5�   6    6B   �� *	IsChecked+	 False6`   .a�5�   :   ��  � ��$4 ./EDLaunch;component/EliteImages/a1Checkbox.png��6Y   5�   7   5�   /   5�   '   �� 5�   &   #��z��60   $	��8,2q�6Q   5�   #   5�      5�      �� 5�      -   5        $ RememberPasswordCheck��5�      $��1��5�      $��0��6(   
�������$(��"/EDlaunch;component/Fonts/#Sintony)�5�      $��16(�5�      $
��Left=�5�      $��0,2,-0.001,0q�66   %   IMGT_RememberPassword#��Z% 5�      ��5�      �� 5�      "! ��6.   .R�5�   "   �� $" 
Horizontal=�6-   .+�5�   &   �� $��20��6,   $��20��67   ��5�   *   �� 5�   .   .�5�   2   ��  � ��$4 ./EDLaunch;component/EliteImages/a1Checkbox.png��6Q    5�   2   �� 5�   6    6B   �� *	IsChecked+# �� "$ �� True6�   .a�5�   :   ��  � ��$: 4/EDLaunch;component/EliteImages/a1CheckboxActive.png��6Y   5�   7   �� 5�   6    6B   �� *	IsChecked+	 False6`   .a�5�   :   ��  � ��$4 ./EDLaunch;component/EliteImages/a1Checkbox.png��6Y   5�   7   5�   /   5�   '   �� 5�   &   #��z��60   $	��8,2q�6Q   5�   #   5�      5�      �� 5�      -   5        & ��Name$& LoginButton��5�        LaunchLoginStyle#��[ 62   $��2��5�      $��0��5�      $��2��6&   $��Right=�67   $	��140��5�      $��45��6%   5�      �� 5�      -   5        $ ForgotPasswordButton��5�       $(��"/EDlaunch;component/Fonts/#Sintony)�5�       $��15(�6P   $��3��5�       $��2��6-   $��Center=�6=   �� 5�      -   5        ��5�   $   �� # Z
 $ DefaultButtonColour�.�6y   .�5�      �� '   LW_ForgotPassword#�Z' 6"   5�      5�      5�      � 5�      -   5        $ VerificationPanel��5�   $   $��3��6   ���#1A1A1A$y�Hidden=�6T    5�      �� 5�      $��* �6+    5�      �� 5�      $
��Auto �6(   �� 5�      $
��Auto �6(   �� 5�      $��* �6(   �� 5      $
��Auto �6(   �� 5     $
��Auto �6(   �� 5     $
��Auto �6(   �� 5     $��* �6(   �� 5     $
��Auto �6(   �� 5     $
��Auto �6(   �� 5     $
��Auto �6(   .+�5	     �� $��0��6    (   LW_CheckEmail#��Z( 6-   $
 Left=�5
     $
��Wrap=�5     
�������$��	0,13,0,10q�5     $*��$/EDlaunch;component/Fonts/#Euro Caps)�5     $��17(�5     5     �� 5     $��1��6   )   LW_CheckEmailDesc#��Z) 5     $
 Left=�5     $
��Wrap=�5     
�������$(��"/EDlaunch;component/Fonts/#Sintony)�5     $��15(�5     �� 5     $��3��6   $
 Left=�5     $
��Wrap=�5     
�������$(��"/EDlaunch;component/Fonts/#Sintony)�5     $��15(�5     �� 5      ��6   �� # Z
 $ DefaultButtonColour�.�6o   •6r    �� 5!     *   LW_CheckEmail1#�Z* 6   5"     �� 5#     $��4��6   $
 Left=�5$     $
��Wrap=�5%     
�������$(��"/EDlaunch;component/Fonts/#Sintony)�5'     $��15(�5(     �� 5)     ��6   �� # Z
 $ DefaultButtonColour�.�6o   •6r    �� 5*     +   LW_CheckEmail2#�Z+ 6   5+     �� 5,     $��5��6   $
 Left=�5-     $
��Wrap=�5.     
�������$(��"/EDlaunch;component/Fonts/#Sintony)�50     $��15(�51     �� 52     ��6   �� # Z
 $ DefaultButtonColour�.�6o   •6r    �� 53     ,   LW_CheckEmail3#�Z, 6   54     �� 56     $��7��6   $��Verification Code��6)   $
 Left=�57     $
��Wrap=�58     
�������$��0,10,0,2q�5:     $*��$/EDlaunch;component/Fonts/#Euro Caps)�5;     $
��Bold"�5<     $��20(�5=     �� 5>     -   5        $ VerificationEdit��5?     $��8��5>     $��0q�5@     $��15(�5A     
����  �$��0,0,0,10q�5C     $(��"/EDlaunch;component/Fonts/#Sintony)�5D     $��5,5,0,5q�5E     
�������6(   �� 5G     -   5        $& SubmitButton��5G       LoginLinkStyle#��[ 6/   $��9��5H     $��Right=�6"   $	��140��5I     $��45��6!   $��0,5,0,0q�5J     5K     5L     5M     5N     5O     