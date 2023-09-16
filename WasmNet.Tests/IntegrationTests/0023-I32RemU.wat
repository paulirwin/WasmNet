;; invoke: rem
;; expect: (i32:2)

(module
    (func (export "rem") (result i32)
        i32.const 6
        i32.const 4
        i32.rem_u
    )
)