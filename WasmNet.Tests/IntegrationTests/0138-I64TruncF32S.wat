;; invoke: trunc
;; expect: (i64:-42)

(module
    (func (export "trunc") (result i64)
        f32.const -42.9
        i64.trunc_f32_s
    )
)