   M S B A M L   `   `   `  ����L  HSharedControls, Version=1.7.6938.1, Culture=neutral, PublicKeyToken=null/clr-namespace:SharedControlsSharedControls  5      #    SharedControls.ProgressStatus   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      +scclr-namespace:SharedControls     5      .��5	      �   � ColumnDefinitions  5
   
   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $��* �6   .+�5   
   �� -   5         ��Name$ ActiveTaskListBox��5      $��0��6-   $��0��6=   $ �� IsSynchronizedWithCurrentItem$
 TrueY�6k   $��Stretch=�6�   $��Disabled=�6�   ��6J   �� *
Monitors+��5      �� 5      .R�5      �� 
�������$��2q�64   $��2q�6H    �� Padding$ 2q�6S   $��Stretch=�6_   ._�5      �   5      �� 5   "   $
��Auto �63    z� SharedSizeGroup$ 
TitleGroup��6@   �� 5   "   $
��Auto �63   $ ActionGroup��6@   �� 5   "   $��* �63   .+�5      ��  ��Name$ ProjectName��64   $��0��6$   ��6I   �� *Name+�� 5      $ 
ActionName��64   $��1��6$   ��6H   �� *Action+� 5       �Name$ ActionProgress��6*   $��2��6B   $��Stretch=�6R    � Maximum 6p   �� *Total+	 � Value	 6�   �� *	Current+5      5      5      
 �� Triggers
 5       �� 5!       �� Value$ False��6G    �� Binding 6&   �� *ShowProgress+.a�5"      ��  �� 
TargetName$ ActionProgress��6%    �� Property � y� �� Value$ Hidden=�6W   5#      �� 5$      $
 True��6G    6&   �� *ShowProgress+.a�5%      �� $ ActionProgress��6%    � y�$ Visible=�6W   5&      5@      5B      5C      5D      