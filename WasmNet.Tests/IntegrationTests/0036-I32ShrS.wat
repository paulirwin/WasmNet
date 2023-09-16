;; invoke: shrs
;; expect: (i32:-11)

(module
    (func (export "shrs") (result i32)
        i32.const -42
        i32.const 2
        i32.shr_s
    )
)