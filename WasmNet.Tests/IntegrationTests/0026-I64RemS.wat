;; invoke: rem
;; expect: (i64:2)

(module
    (func (export "rem") (result i64)
        i64.const 6
        i64.const 4
        i64.rem_s
    )
)