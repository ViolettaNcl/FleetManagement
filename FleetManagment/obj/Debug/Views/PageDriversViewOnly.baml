   M S B A M L   `   `   `  ����
  Zachet&     Zachet.Views.PageDriversViewOnly   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Aav2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5        N� Title$  PageDriversViewOnly��5      .��5      � ���#F0F0FF$��Stretch=�6    $��Stretch=�6>    � RowDefinitions 5   
   �� 5      $	��100 �6   �� 5      $��* �6   .+�5   
   �� $%��Список водителей��6   $��24(�5      $
��Bold"�6"   ���#6A0DAD$��Center=�5      $	��Top=�61   $��0,10,0,0q�6I   �� 5   
    �� Orientation$ 
Horizontal=�6   $��Right=�6.   $��Center=�6J   $��0,0,20,0q�6e   .+�5      �� -   5        $��40��5      $	��120��6!   ���#FF6F61
�������$
��Bold"�5      ���#483D8B$��2q�6=   $
��10,5q�5      $��0,0,10,0q�6$    � Cursor$
 Hand��66   $��
Назад��6D   5      �� 5   
   $ 
Horizontal=�6   $
��Left=�6.   $��Center=�6I   $��20,0,0,0q�6d   .+�5      �� -   5         ��Name$ SortComboBox��5      $	��120��6-   $��25��69   $��0,0,5,0q�6E   ���#E6E6FA���#483D8B���#6A5ACD$��1q�5    -   .@�5!      �� $,��&По возрастанию стажа��6   
 � Tag$ 	Ascending��6>   �� 5"      $&�� По убыванию стажа��6   $ 
Descending��6;   5#      �� 5$      -   5        ���#4682B4
�������$
��Bold"�5&      ���#483D8B$��2q�6=   $	��5,2q�5'      $��25��6#   $	��120��6/   $
 Hand��6;   $��Сортировать��6I   5(      &   System.Windows.Controls.DataGrid  5*   
   -   5        $��DriversGrid��5,      $��1��5*         
IsReadOnly .    CanUserAddRows .     CanUserDeleteRows .  	   AutoGenerateColumns	 .  
�������
   AlternatingRowBackground
 �#EEEAF8���#6A5ACD$��2q�5.   L   $��20,10,20,20q�5/      $��Stretch=�6(   $��Stretch=�6F      Columns 50      0  *System.Windows.Controls.DataGridTextColumn  51      ,  &System.Windows.Controls.DataGridColumn   Header$ Id��6%      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 6H   1  +System.Windows.Controls.DataGridBoundColumn   Binding 61   �� *Id+6H     52      $ ФИО��6%   $ * 6O    62   �� *
FullName+6O     53      $! Номер лицензии��6%   $ * 6_    6=   �� *LicenseNumber+6_     54      $ Телефон��6%   $ * 6V    66   �� *PhoneNumber+6V     55      $ Стаж��6%   $ * 6R    63   �� *
Experience+6R   57      58      59      