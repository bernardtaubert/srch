   M S B A M L   `   `   `  ����  Srch    Srch.SearchFilesWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � WindowStartupLocation$  CenterScreen=�5   	    � 
ResizeMode$ NoResize=�5   	   $��0q�5   	    � Title$ Search Files��5
   	   $��137.868��6   $��272.8��6/   .��5      � $��	0,0,0.4,2q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   .+�5   
   �� -   5         ��Name$ cbSearchBox��5   0   $��0��6   $��0��6    $��Stretch=�6E   $��10,10,10,-0.4q�6c   $	��Top=�6�   $
��Auto��6�    �� 
IsEditable . $��18��6�   �� 5   
   -   5         ��Name$ tbSearchBox��5   /   $��0��6   $��0��6   $��Stretch=�6D   $��18��6b   $��
10,10,27,0q�6n    �� TextWrapping$ NoWrap=�6�   	 �� AcceptsReturn	 .  $	��Top=�6�   $
��Auto��6�   
 � ToolTip$'
 !Search for files using .NET RegEx��6�   �� 5   
   -   5         ��Name$! cbSearchFilesSubDirectories��5   0   $��1��6   $��0��6    $%��search files in sub directories��6�   $
��Left=�6�   $��
10,5.2,0,0q�6  $	��Top=�6  $
��Auto��60  $
��Auto��6=  �� 5   
   -   5        $ tbFilePattern��5   /   $��3��6   $��0��6   $��Stretch=�6F   $��18��6d   $��11,0.6,10,10q�6p   $_
 YOnly consider Files that match a certain pattern, use , as delimiter and *. as wildcards.��6�   $ NoWrap=�6�   	 .  $	��Top=�6  $	��246��6.  �� 5   
   $��2��6   $��0��6   $��file filter��6-   $
��Left=�6C   $��10,4,0,0q�6^   $	��Top=�6p   $
��Auto��6�   $
��Auto��6�   5      5      