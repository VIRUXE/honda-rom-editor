                org 0000h
                INCLUDE "config.inc"
int_start_vec:  DW  code_start
                DS  36h, 0FFh
code_start:     MOV SSP, #047Eh
feat_x:         IF FEATURE_X
                DB  011h, 022h, 033h, 044h
                ELSE
                RT
                DS  3, 0FFh
                ENDIF
feat_x_end:     ASSERT $ == feat_x + 4, "feat_x length changed"
                RT

                org 0100h
tbl_fuel:       DB  10, 20, 30, 40, 50, 60      ; 2 rows x 3 cols
tbl_fuel_rpm:   DB  003h,0E8h, 007h,0D0h        ; row axis: 1000, 2000 (editor words are big-endian)
tbl_fuel_map:   DB  000h,01Eh, 000h,03Ch, 000h,05Ah  ; col axis: 30, 60, 90
rev_limit:      DB  020h,008h                   ; 8200 rpm, big-endian
