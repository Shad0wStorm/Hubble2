   M S B A M L   `   `   `  ����  ManifestTool%    ManifestTool.ManifestToolWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      -   5        $��
ManifestUI��5        � Title$  Manifest Tool��5      $	��350��6   $	��725��6&   .��5      � $��4q�6    � ColumnDefinitions 5      �� 5      $
��Auto �6   �� 5	      $��* �6   �� 5
      $
��Auto �6    � RowDefinitions 5      �� 5      $
��Auto �6   �� 5      $��* �6   .+�5      �� $��File Store :��6
   $��0��6!   $��0��6.   $��4q�6>   �� 5      -   5        $��m_fileStores��5      $��0��6�   $��1��6�   $��4q�6�    �� SelectedValue 6!   ��  �� ElementName$ 
ManifestUI�� �� Path$ WorkingDirectory���6i   �� $ 
ManifestUI��$ 
FileStores�6�   �� 5      -   5        $��	Browse...��5      $��0��6   $��2��6,   $��4q�6<   6G   � 5      $��1��6	   $��0��6   $��3��6&   $��4q�6:    5      �� 5      $��* �6   �� 5      $
��Auto �6    5      �� 5      $
��Auto �6   �� 5      $��* �6   .+�5      ��  �� Orientation$ Vertical=�6   $��0��6'   $��0��64   $��2��6D   $��8q�6X   .+�5      �� $ 
Horizontal=�6   .+�5      �� $��File store contains ��6   �� 5      ��6   �� $ 
ManifestUI��$ ActiveFileStore.FileCount��� 5       $�� files totaling ��6   �� 5!      ��6   �� $ 
ManifestUI��$ ActiveFileStore.FileSize��� 5"      $��.��6   5#      �� 5$      $ 
Horizontal=�6   .+�5%      �� $��Manifests contain ��6   �� 5&      ��6   �� $ 
ManifestUI��$$ ActiveFileStore.TotalFileCount��� 5'      $�� files totaling ��6   �� 5(      ��6   �� $ 
ManifestUI��$# ActiveFileStore.TotalFileSize��� 5)      $��.��6   5*      5+      �� 5,      -   5        $��m_manifestList��5,      $��1��6#   $��0��60   ��5-      �� $ 
ManifestUI��$" ActiveFileStore.ManifestList���5/      �� 50      .R�51      �� .+�52   	   �� $
��Bold"�62   ��6   �� *ManifestTitle+62   �� 59   	   $ 
Horizontal=�6   .+�5:   
   �� $��	Contains ��6   �� 5;   
   ��6   �� *	FileCount+�� 5<   
   $�� files (��6   �� 5=   
   ��6   �� *TotalSizePretty+�� 5>   
   $��) comprising ��6   �� 5?   
   ��6   �� *UniqueCount+�� 5@   
   $�� distinct files (��6   �� 5A   
   ��6   �� *UniqueSizePretty+�� 5B   
   $	��). ��6   �� 5C   
   ��5D   
   �� *LocalFileCount+�� 5E   
   $�� files (��5F   
   �� 5G   
   ��5H   
   �� *LocalSizePretty+�� 5I   
   $$��) are unique to this manifest.��5J   
   5K   
   5L   	   5M      5O      � 5P      $��1��6
   $��1��6   $��4q�6'    5Q      �� 5R      $��* �6   �� 5S      $
��Auto �6   .+�5U      �� $��0��6   .+�5V      �� -   5        $	��Add��5V      $	��0,8q�6   {�6)   �� $ 
ManifestUI��$ AddPermitted�6i   �� 5W      -   5        $��Validate��5W      $	��0,8q�6!   {�6.   �� $ 
ManifestUI��$ ValidatePermitted�6s   �� 5X      -   5        $��Export��5X      $	��0,8q�6   {�6,   �� $ 
ManifestUI��$ ExportPermitted�6o   5Y      �� 5Z      -   5        $��1��5Z      $��Delete��6   $��8q�6+   {�66   �� $ 
ManifestUI��$ DeletePermitted�6y   5[      5\      5]      5^      