   M S B A M L   `   `   `  ����
  Zachet    Zachet.Views.PageDrivers   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Aav2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5        N� Title$  PageDrivers��5      .��5      � ���#F0F0FF$��Stretch=�6    $��Stretch=�6>    � RowDefinitions 5   
   �� 5      $��80 �6   �� 5      .+�5   
   ��  �� Orientation$ 
Horizontal=�6   $��Right=�6.   $��Center=�6J   $��0,0,20,0q�6e   .+�5      �� -   5        $��40��5      $	��150��6!   ���#7B68EE
�������$
��Bold"�5      ���#483D8B$��2q�6=   $
��10,5q�5      $��0,0,10,0q�6$    � Cursor$
 Hand��66   $��Обновить��6D   �� 5      -   5        $��40��5      $	��150��6!   ���#6A0DAD
�������$
��Bold"�5      ���#483D8B$��2q�6=   $
��10,5q�5      $��0,0,20,0q�6$   $
 Hand��66   $'��!Добавить водителя��6D   �� 5      -   5        $��40��5      $	��100��6!   ���#FF6F61
�������$
��Bold"�5!      ���#483D8B$��2q�6=   $
��10,5q�5"      $
 Hand��6$   $��
Назад��62   5#      &   System.Windows.Controls.DataGrid  5%   
   -   5        $��DriversGrid��5'      $��1��5%         
IsReadOnly .    CanUserAddRows .     CanUserDeleteRows .     AutoGenerateColumns .  
�������   AlternatingRowBackground �#EEEAF8���#6A5ACD$��2q�5)   L   $��20,10,20,20q�5*      	   Columns	 5+      0  *System.Windows.Controls.DataGridTextColumn  5,      ,  &System.Windows.Controls.DataGridColumn
   Header$
 Id��6%      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 6H   1  +System.Windows.Controls.DataGridBoundColumn   Binding 61   �� *Id+6H     5-      $
 ФИО��6%   $ * 6O    62   �� *
FullName+6O     5.      $!
 Номер лицензии��6%   $ * 6_    6=   �� *LicenseNumber+6_     5/      $
 Телефон��6%   $ * 6V    66   �� *PhoneNumber+6V     50      $
 Стаж��6%   $ * 6R    63   �� *
Experience+6R   4  .System.Windows.Controls.DataGridTemplateColumn  51      $"
 Редактирование��6)   $
 Auto 6A      CellTemplate 52      �� 53      .R�54      �� -   5        ���#9370DB
����������#483D8B$��2q�55   ;   $
��Bold"�56   D   $	��5,2q�6V   $
 Hand��6d   $ ��Редактировать��6r   57      59        5:      $
 Удаление��6)   $
 Auto 6;    5;      �� 5<      .R�5=      �� -   5        ���#BA55D3
����������#8A2BE2$��2q�5>   ;   $
��Bold"�5?   F   $	��5,2q�6X   $
 Hand��6f   $��Удалить��6t   5@      5B      5D      5E      5F      