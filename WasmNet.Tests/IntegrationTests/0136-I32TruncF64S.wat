;; invoke: trunc
;; expect: (i32:-42)

(module
    (func (export "trunc") (result i32)
        f64.const -42.9
        i32.trunc_f64_s
    )
)