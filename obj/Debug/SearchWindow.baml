   M S B A M L   `   `   `  ����  Srch    Srch.SearchWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     6F   -   5          � WindowStartupLocation$  CenterScreen=�5   	    � 
ResizeMode$ NoResize=�5   	   $��0q�5	   	    � Title$ Search in Files��5   	   $��197.467��6!   $��322.267��62   .��5      � $��	0,0,0.4,2q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      .+�5   
   �� -   5         ��Name$ cbSearchBox��5   D   $��0��6   $��0��6    $��2��60   $��Stretch=�6Y   $��10,10,10,-0.4q�6w   $	��Top=�6�   $
��Auto��6�    �� 
IsEditable . $��18��6�   �� 5   
   -   5         ��Name$ tbSearchBox��5   C   $��0��6   $��0��6   $��2��6/   $��Stretch=�6X   $��18��6v   $��
10,10,27,0q�6�    �� TextWrapping$ NoWrap=�6�   $	��Top=�6�   $
��Auto��6�   	 �� AcceptsReturn	 .  6�   �� 5   
   -   5        $ tbFilePattern��5   D   $��6��6   $��0��6   $��2��6/   $��Stretch=�6[   $��18��6y   $��
10,4,10,10q�6�   	 .  
 � ToolTip$_
 YOnly consider Files that match a certain pattern, use , as delimiter and *. as wildcards.��6�   $ NoWrap=�6  $	��Top=�6)  $
��Auto��6A  �� 5   
   -   5         ��Name$ cbCaseSensitive��5   1   $��1��6   $��1��6    $��case-sensitive��6�   $
��Left=�6�   $��	7.2,6,0,0q�6�   $	��Top=�6�   $
��Auto��6�   $
��Auto��6	  �� 5   
   -   5        $ cbOnlyShow1EntryPerLine��5   1   $��2��6   $��1��6    {�. $$��only show first result in line��6�   $
��Left=�6�   $��7.2,7.2,10,0q�6	  $	��Top=�6  $
��Auto��67  $+
 %Drop further results in the same line��6D  $
��Auto��6t  �� 5   
   -   5        $ cbSearchSubDirectories��5   1   $��3��6   $��1��6    $��search sub directories��6�   $
��Left=�6�   $��7.2,7.4,0,0q�6�   $	��Top=�6  $
��Auto��6  $
��Auto��6'  	� 5    
   -   5         	�Name$ 	rbDefault��5    4   $��1��6   $��0��6#   $��default��6G   $
��Left=�6Y   $��
10,7.4,0,0q�6t   $	��Top=�6�   $D
 >The default search option is not supporting RegEx, but fastest��6�   $
��Auto��6  $
��Auto��6  	� 5!   
   -	   5        $ rbWholeWordsOnly��5!   4   $��2��6   $��0��6#   $��whole words only��6N   $��
10,7.4,0,0q�6i   $	��Top=�6}   $
��Auto��6�   	� 5"   
   -
   5        $ rbFastRegEx��5"   4   $��3��6   $��0��6#   $��RegEx .*^$\��6I   $
��Left=�6_   $��
10,7.4,0,0q�6z   $	��Top=�6�   $[
 UCustom fast and simplified (POSIX style) RegEx only allowing .*^$\ (line-wise search)��6�   $
��Auto��6$  $
��Auto��62  	� 5#   
   -   5        $ 
rbNETRegEx��5#   4   $��4��6   $��0��6#   $��
RegEx .NET��6H   $
��Left=�6]   $��
10,7.4,0,0q�6x   $	��Top=�6�   $P
 JWarning: .NET RegEx severely slow down the performance (line-wise search).��6�   $
��Auto��6  $
��Auto��6%  �� 5$   
   $��5��6   $��0��6   $��file filter��6.   $
��Left=�6D   $��10,0.4,0,17q�6_   $	��Top=�6t   $
��Auto��6�   $
��Auto��6�   $��2��6�   5%      5&      